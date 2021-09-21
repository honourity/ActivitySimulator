using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ActivitySimulator
{
    class Program
    {
        //13 activities takes about 6 seconds non-threaded (14 activities takes 30 seconds)
        //13 activities takes about 2 seconds threaded (14 activities takes 6.5 seconds)
        static string _inputFileName = null;

        static readonly Random _random = new();

        static void Main(string[] args)
        {
            var random = false;
            var iterations = 10000;

            if (args.Length > 1 && !string.IsNullOrEmpty(args[1]))
            {
                if (args.Length > 2)
                {
                    _ = int.TryParse(args[1], out iterations);
                }

                if (args[0] == "-r")
                {
                    random = true;
                }

                _inputFileName = args[^1];
            }
            else
            {
                if (args.Length > 0 || !string.IsNullOrEmpty(args[^1]))
                {
                    _inputFileName = args[^1];
                }
                else
                {
                    Console.WriteLine("Please provide an input CSV file eg: ActivitySimulator.exe [-r] input.csv\nOptionally -r before input.csv to generate randomised output instead of simulated");
                }
            }

            if (string.IsNullOrEmpty(args[^1]))
            {
                Console.WriteLine("Please provide an input CSV file eg: ActivitySimulator.exe [-r] input.csv\nOptionally -r before input.csv to generate randomised output instead of simulated");
            }
            else
            {
                var activities = LoadActivities();

                if (random)
                {
                    PerformTaskRandom(activities, iterations);
                }
                else
                {
                    PerformTaskNonrepeating(activities);
                }
            }
        }

        static Activity[] LoadActivities()
        {
            var fileContents = File.ReadAllLines(_inputFileName);
            var rows = fileContents.Where(f => !string.IsNullOrWhiteSpace(f)).ToList();
            rows.RemoveAt(0);
            //skip header row, reduce by 1, start at 1 for loop
            var activities = new Activity[rows.Count()];

            for (var row = 0; row < rows.Count(); row++)
            {
                var line = rows[row];
                var split = line.Split(',');
                activities[row] = new Activity()
                {
                    ActivityNumber = int.Parse(split[0]),
                    Durations = new decimal[] { int.Parse(split[1]), int.Parse(split[2]), int.Parse(split[3]) },
                    Probabilities = new decimal[] { decimal.Parse(split[4]), decimal.Parse(split[5]), decimal.Parse(split[6]) }
                };
            }

            Console.WriteLine("Loaded " + activities.Length + " activities from " + _inputFileName);

            return activities;
        }

        static void PerformTaskRandom(Activity[] activities, int iterations)
        {
            var results = AssembleActivityDurationOperationsRandom(activities, iterations);

            results = results.OrderBy(operationActivities => long.Parse(string.Join(string.Empty, operationActivities.Select(operationActivity => operationActivity[1])))).ToList();

            CalculateExpectedDuration(activities, results);
        }

        static void PerformTaskNonrepeating(Activity[] activities)
        {
            var results = AssembleActivityDurationOperations(activities).Result;

            CalculateExpectedDuration(activities, results);
        }

        static async Task<List<List<int[]>>> AssembleActivityDurationOperations(Activity[] activities)
        {
            var timer = new Stopwatch();
            timer.Start();

            Console.WriteLine("Assembling full list of activity & duration combinations...");

            var numDurations = activities[0].Durations.Length;

            //theoretically the number of possible outcomes
            var completeIterations = (ulong)Math.Round(Math.Pow(numDurations, activities.Length));

            var results = new List<List<int[]>>();

            var tasks = new List<Task<List<List<int[]>>>>();
            var jobSize = completeIterations;

            var cores = (ulong)Environment.ProcessorCount;

            ////uncomment this to enable multi-threading - needs activityDurationIndexs to be carefully calculated and created in each thread,
            //// so each thread can start at index 0 without causing skipping over of values or repeat results
            //if (completeIterations > cores)
            //{
            //    jobSize = completeIterations / cores;
            //}

            var activityDurationIndexs = new int[activities.Length];

            ulong i = 0;
            while (i < completeIterations)
            {
                ulong endIndex;

                if (completeIterations - i < jobSize)
                {
                    endIndex = completeIterations;
                }
                else
                {
                    endIndex = i + jobSize;
                }

                tasks.Add(ThreadAssembleOperations(activities, i, endIndex, numDurations, activityDurationIndexs));

                i = endIndex;
            }

            foreach (var task in tasks)
            {
                results.AddRange(await task);
            }

            timer.Stop();

            Console.WriteLine("Assembled " + results.Count() + " different combinations (took " + timer.Elapsed.TotalSeconds.ToString("0.##") + " seconds)");

            return results;
        }

        static List<List<int[]>> AssembleActivityDurationOperationsRandom(Activity[] activities, int iterations)
        {
            var timer = new Stopwatch();
            timer.Start();

            Console.WriteLine("Assembling full list of activity & duration combinations...");

            //a list of all scenarios in this randomly chosen subset simulation
            var results = new List<List<int[]>>();

            for (var i = 0; i < iterations; i++)
            {
                //a whole scenario, list of every activity with a single duration choice
                var result = new List<int[]>();

                for (var j = 0; j < activities.Count(); j++)
                {
                    var roll = (decimal)_random.NextDouble();
                    if (roll <= activities[j].Probabilities[0])
                    {
                        //one activity index with one duration index
                        result.Add(new int[2] { j, 0 });
                    }
                    else if (roll <= activities[j].Probabilities[0] + activities[j].Probabilities[1])
                    {
                        //one activity index with one duration index
                        result.Add(new int[2] { j, 1 });
                    }
                    else
                    {
                        //one activity index with one duration index
                        result.Add(new int[2] { j, 2 });
                    }
                }

                results.Add(result);
            }

            timer.Stop();

            Console.WriteLine("Assembled " + results.Count() + " different combinations (took " + timer.Elapsed.TotalSeconds.ToString("0.##") + " seconds)");

            return results;
        }

        static async Task<List<List<int[]>>> ThreadAssembleOperations(Activity[] activities, ulong iterationsStart, ulong iterationsStop, int numDurations, int[] activityDurationIndexs)
        {
            return await Task.Run(() =>
            {
                var results = new List<List<int[]>>();
                var activityDurationIndexesLength = (ulong)activityDurationIndexs.Length;
                var activitiesLength = (ulong)activities.Length;

                for (ulong i = iterationsStart; i < iterationsStop; i++)
                {
                    for (ulong j = 0; j < activityDurationIndexesLength; j++)
                    {
                        if (i % (Math.Pow(numDurations, activitiesLength - j - 1)) == 0 && i != 0)
                        {
                            if (activityDurationIndexs[j] == numDurations - 1)
                            {
                                activityDurationIndexs[j] = 0;
                            }
                            else
                            {
                                activityDurationIndexs[j]++;
                            }
                        }
                    }

                    var set = new List<int[]>();
                    for (var j = 0; j < activities.Count(); j++)
                    {
                        var activity = new int[] { j, activityDurationIndexs[j] };

                        set.Add(activity);
                    }
                    results.Add(set);
                }

                return results;
            });
        }

        static void CalculateExpectedDuration(Activity[] activities, List<List<int[]>> results)
        {
            Console.WriteLine("Performing duration/probability calculations on all combinations...");

            var outputFilename = _inputFileName.Split(".csv")[0];
            outputFilename += "_output.csv";

            using StreamWriter writer = File.CreateText(outputFilename);

            //setting up csv header row
            writer.Write("Scenario Combinations,");
            foreach (var activity in activities)
            {
                writer.Write("D(" + activity.ActivityNumber + "),");
            }
            writer.Write("Total Duration,");
            foreach (var activity in activities)
            {
                writer.Write("P(" + activity.ActivityNumber + "),");
            }
            writer.Write("Expected Probability,Expected Duration\n");

            decimal totalExpectedDuration = 0;

            foreach (var operation in results)
            {
                decimal subtotalDuration = 0;
                decimal? subtotalExpectedProbability = null;
                decimal subtotalExpectedDuration = 0;

                foreach (var value in operation)
                {
                    subtotalDuration += activities[value[0]].Durations[value[1]];

                    if (subtotalExpectedProbability == null)
                    {
                        subtotalExpectedProbability = activities[value[0]].Probabilities[value[1]];
                    }
                    else
                    {
                        subtotalExpectedProbability *= activities[value[0]].Probabilities[value[1]];
                    }
                }

                if (subtotalExpectedProbability != null)
                {
                    subtotalExpectedDuration = subtotalDuration * subtotalExpectedProbability.Value;
                }

                //enter row for csv output
                foreach (var value in operation)
                {
                    writer.Write(value[1]);
                }
                writer.Write(",");
                foreach (var value in operation)
                {
                    writer.Write(activities[value[0]].Durations[value[1]]);
                    writer.Write(",");
                }
                writer.Write(subtotalDuration + ",");
                foreach (var value in operation)
                {
                    writer.Write(activities[value[0]].Probabilities[value[1]] + ",");
                }
                if (subtotalExpectedProbability != null) writer.Write(subtotalExpectedProbability.Value.ToString("0.############################"));
                writer.WriteLine("," + subtotalExpectedDuration.ToString("0.############################"));

                totalExpectedDuration += subtotalExpectedDuration;
            }

            Console.WriteLine("Calculations written to \"" + outputFilename + "\"");

            Console.WriteLine("Expected Duration: " + totalExpectedDuration.ToString("0.############################"));

            Console.WriteLine("DONE!\n");
        }
    }

    class Activity
    {
        public int ActivityNumber;

        public decimal[] Durations;
        public decimal[] Probabilities;
    }

    class Result
    {
        public decimal Duration;
        public int Occurences;
        public List<Activity> ActivitySet = new();
    }
}
