using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

namespace TravelingSalesman {
    public class Salesman {

        // 19 cities = 1.216451e^17 possible paths

        private int numCities = 0; // number of cities on map
        private List<string[]> cities = new List<string[]>();

        private int _maxIterations; // maximum number of iterations
        private int _currentIteration; // current iteration
        private double _startTemp; // starting temperature
        private double _currentTemp; // current temperature
        private double _endTemp; // final temperature
        private bool _finished = false; // gone through all iterations?

        private double _currentScore; // current score
        private double _prevScore; // previous score
        private double _bestScore = double.MaxValue; // best score
        private double _lastProbability; // probability of improving score

        private int _cycles = 100; // number of cycles in iteration


        private double[][] _cities; // all cities
        private int[] _currentPath, _bestPath, _backUpPath; // current path, best path, back up path
        private Random _rand = new Random(); // new random object

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="maxIterations">Maximum number of iterations to perform.</param>
        /// <param name="startTemp">Starting temperature for annealing process.</param>
        /// <param name="endTemp">Ending temperature for annealing process.</param>
        public Salesman(int maxIterations, double startTemp, double endTemp) {
            _maxIterations = maxIterations;
            _startTemp = startTemp;
            _endTemp = endTemp;
        }

        /// <summary>
        /// Entry point for the program.
        /// </summary>
        public void Run() {

            // get file from resource stream
            var f = Assembly.GetExecutingAssembly().GetManifestResourceStream("TravelingSalesman.graph.txt");
            StreamReader sr = new StreamReader(f);

            using (sr) {
                while(!sr.EndOfStream) {
                    string[] data = sr.ReadLine().Split(','); // get data from line
                    cities.Add(data); // add city data to list of cities
                    numCities++; // number of cities is increased
                }
            }
            sr.Close();

            // allocate memory
            _cities = new double[numCities][];
            _currentPath = new int[numCities];
            _bestPath = new int[numCities];
            _backUpPath = new int[numCities];

            // create random path to start
            for (int i = 0; i < _currentPath.Length; i++) {

                int city; // index of city
                bool done; // done with cycle?

                do {

                    city = _rand.Next(numCities); // random index
                    done = true; // default to true

                    // make sure city isn't already in _currentPath
                    for (int j = 0; j < i; j++) {
                        if (city == _currentPath[j]) {
                            done = false;
                        }
                    }

                } while (!done);

                _currentPath[i] = city; // place city in current path
            }

            while(!_finished) { // while current iteration < max iterations

                Iteration(); // perform iteration
                
                // finish
                if(_currentIteration >= _maxIterations) {
                    _finished = true;
                }

            }
                
            // display results
            Console.WriteLine("Best path:");
            for (int i = 0; i < _bestPath.Length; i++) {
                Console.Write((cities[_bestPath[i]])[0]);
                if(i < _bestPath.Length-1) {
                    Console.Write("-->");
                }
            }
            Console.WriteLine();

        }

        /// <summary>
        /// Runs one iteration of the annealing process.
        /// </summary>
        private void Iteration() {

            // if first iteration, then initialize
            if(_currentIteration == 0) {
                _currentScore = Evaluate();
                Array.Copy(_currentPath, _bestPath, _currentPath.Length);
                _prevScore = _currentScore;
                _bestScore = _currentScore;
            }

            _currentIteration++; // increase iteration number

            _currentTemp = Cooling(); // get current temperature

            // loop through cycles
            for(int cycle = 0; cycle < _cycles; cycle++) {

                Array.Copy(_currentPath, _backUpPath, _currentPath.Length); // make backup copy
                
                Randomize(); // randomize the path

                bool keep = false; // should we keep the new path?

                double trialScore = Evaluate(); // get a score for the new path

                if(trialScore < _currentScore) { // if new path is shorter

                    keep = true; // then we keep

                } else { // else if probability is greater than random number, then we also keep
                    
                    _lastProbability = CalculateProbability(trialScore, _currentScore, _currentTemp);
                    if(_lastProbability > _rand.NextDouble()) {
                        keep = true;
                    }

                }

                // if we should keep new path,
                // then change all necessary values
                if(keep) {
                    _currentScore = trialScore;
                    if(_currentScore < _bestScore) {
                        _bestScore = _currentScore;
                        Array.Copy(_currentPath, _bestPath, _currentPath.Length);
                    } else {
                        Array.Copy(_backUpPath, _currentPath, _currentPath.Length);
                    }
                }

            }

            // print out best score every time it changes
            if(_bestScore != _prevScore) {
                Console.WriteLine("Iteration #" + _currentIteration + " Shortest Path:" + Math.Round(_bestScore, 2) + " miles");
            }

            _prevScore = _bestScore;
        }

        /// <summary>
        /// Randomizes the path of the cities.
        /// </summary>
        private void Randomize() {

            // get random cities to switch
            int r = _rand.Next(numCities), r2 = _rand.Next(numCities);
            while (r2 == r) {
                r2 = _rand.Next(numCities);
            }

            // switch the cities
            int temp = _currentPath[r];
            _currentPath[r] = _currentPath[r2];
            _currentPath[r2] = temp;
        }

        /// <summary>
        /// Finds a score for the current path.
        /// </summary>
        /// <returns>Path score.</returns>
        private double Evaluate() {
            double sum = 0;
            // iterate through every city i,
            // getting the distance between i and i + 1
            for(int i = 0; i < numCities - 1; i++) {

                string[] city = cities[_currentPath[i]];
                string[] city2 = cities[_currentPath[i + 1]];

                sum += GetDistance(city, city2);
            }

            return sum; // return total distance
        }

        /// <summary>
        /// Perform cooling of the temperature.
        /// </summary>
        /// <returns>Current temperature.</returns>
        private double Cooling() {
            double exp = (double)_currentIteration / _maxIterations; // exponent for equation
            return _startTemp * Math.Pow(_endTemp/_startTemp, exp); // equation
        }

        /// <summary>
        /// Calculate the probability of achieving a better score.
        /// </summary>
        /// <param name="currScore">Current score.</param>
        /// <param name="prevScore">Previous score.</param>
        /// <param name="currTemp">Current temperature.</param>
        /// <returns>Probability.</returns>
        private double CalculateProbability(double currScore, double prevScore, double currTemp) {
            return Math.Exp(-(Math.Abs(prevScore - currScore)) / currTemp);
        }

        /// <summary>
        /// Gets distance between two coordinates by using the Haversine formula.
        /// </summary>
        /// <param name="pos1">Coordinate 1.</param>
        /// <param name="pos2">Coordinate 2.</param>
        /// <returns>Distance in miles.</returns>
        private double GetDistance(string[] pos1, string[] pos2) {
            double lat1 = Convert.ToDouble(pos1[1]);
            double lat2 = Convert.ToDouble(pos2[1]);
            double long1 = Convert.ToDouble(pos1[2]);
            double long2 = Convert.ToDouble(pos2[2]);

            double dLong = DegreeToRadians(long2 - long1);
            double dLat =  DegreeToRadians(lat2 - lat1);

            double a = 
                Math.Sin(dLat/2) * Math.Sin(dLat/2) + 
                Math.Cos(DegreeToRadians(lat1)) * Math.Cos(DegreeToRadians(lat2)) *
                Math.Sin(dLong/2) * Math.Sin(dLong/2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            double d = 3961 * c;
            return d;
        }

        /// <summary>
        /// Converts degrees to radians.
        /// </summary>
        /// <param name="deg">Decimal degree.</param>
        /// <returns>Radian equivalent of decimal degree.</returns>
        private double DegreeToRadians(double deg) {
            return deg * (Math.PI/180);
        }
    }
}
