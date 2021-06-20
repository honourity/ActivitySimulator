﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ActivitySimulator
{
    class Program
    {
        static void Main(string[] args)
        {
            //13 activities takes about 6 seconds non-threaded (14 activities takes 30 seconds)
            //13 activities takes about 2 seconds threaded (14 activities takes 6.5 seconds)


            var activities = LoadActivities();

            //PerformTaskRandom(activities);

            PerformTaskNonrepeating(activities);
        }

        static Activity[] LoadActivities()
        {
            var fileContents = File.ReadAllLines("input.csv");

            //skip header row, reduce by 1, start at 1 for loop
            //var activities = new Activity[fileContents.Length - 1];

            //for (var row = 1; row < fileContents.Length; row++)
            //{
            //    var line = fileContents[row];
            //    var split = line.Split(',');
            //    activities[row - 1] = new Activity()
            //    {
            //        ActivityNumber = int.Parse(split[0]),
            //        Durations = new int[] { int.Parse(split[1]), int.Parse(split[2]), int.Parse(split[3]) },
            //        Probabilities = new double[] { double.Parse(split[4]), double.Parse(split[5]), double.Parse(split[6]) }
            //    };
            //}

            var activities = new Activity[]
            {
                new Activity() {
                    ActivityNumber = 1,
                    Durations = new int[] { 4, 8, 10 },
                    Probabilities = new double[] { 0.15, 0.50, 0.35 },
                },
                new Activity() {
                    ActivityNumber = 2,
                    Durations = new int[] { 1, 2, 4 },
                    Probabilities = new double[] { 0.25, 0.50, 0.25 },
                },
                new Activity() {
                    ActivityNumber = 3,
                    Durations = new int[] { 4, 8, 10 },
                    Probabilities = new double[] { 0.15, 0.50, 0.35 },
                },
                new Activity() {
                    ActivityNumber = 4,
                    Durations = new int[] { 1, 2, 4 },
                    Probabilities = new double[] { 0.25, 0.50, 0.25 },
                },
                new Activity() {
                    ActivityNumber = 5,
                    Durations = new int[] { 4, 8, 10 },
                    Probabilities = new double[] { 0.15, 0.50, 0.35 },
                },
                new Activity() {
                    ActivityNumber = 6,
                    Durations = new int[] { 1, 2, 4 },
                    Probabilities = new double[] { 0.25, 0.50, 0.25 },
                },
                new Activity() {
                    ActivityNumber = 7,
                    Durations = new int[] { 4, 8, 10 },
                    Probabilities = new double[] { 0.15, 0.50, 0.35 },
                },
                new Activity() {
                    ActivityNumber = 8,
                    Durations = new int[] { 1, 2, 4 },
                    Probabilities = new double[] { 0.25, 0.50, 0.25 },
                },
                new Activity() {
                    ActivityNumber = 9,
                    Durations = new int[] { 4, 8, 10 },
                    Probabilities = new double[] { 0.15, 0.50, 0.35 },
                },
                new Activity() {
                    ActivityNumber = 10,
                    Durations = new int[] { 4, 8, 10 },
                    Probabilities = new double[] { 0.15, 0.50, 0.35 },
                },
                new Activity() {
                    ActivityNumber = 11,
                    Durations = new int[] { 4, 8, 10 },
                    Probabilities = new double[] { 0.15, 0.50, 0.35 },
                },
                new Activity() {
                    ActivityNumber = 12,
                    Durations = new int[] { 4, 8, 10 },
                    Probabilities = new double[] { 0.15, 0.50, 0.35 },
                },
                new Activity() {
                    ActivityNumber = 13,
                    Durations = new int[] { 4, 8, 10 },
                    Probabilities = new double[] { 0.15, 0.50, 0.35 },
                },
                new Activity() {
                    ActivityNumber = 14,
                    Durations = new int[] { 4, 8, 10 },
                    Probabilities = new double[] { 0.15, 0.50, 0.35 },
                },
            };

            return activities;
        }

        static void PerformTaskRandom(Activity[] activities)
        {
            var iterations = 1000000d;

            Console.WriteLine("Performing random probablistic duration calculations with " + iterations + " iterations");

            var random = new Random();
            var results = new List<Result>();
            
            for (var i = 0d; i < iterations; i++) { PerformTaskRandomIteration(activities, results, random); }

            var ordered = results.OrderBy(r => r.Duration);
            var expectedDuration = 0d;
            foreach (var result in ordered)
            {
                expectedDuration += result.Duration * (result.Occurences / iterations);
                //Console.WriteLine("Duration:" + result.Duration + " Proportion: " + (result.Occurences / iterations));
            }

            Console.WriteLine("Expected Duration: " + Math.Round(expectedDuration, 3));

            Console.WriteLine("DONE!\n");
        }

        static void PerformTaskRandomIteration(Activity[] activities, List<Result> results, Random random)
        {
            var duration = 0d;
            foreach (var activity in activities)
            {
                //add up probabilities and compare with random roll, a bit like a pie chart
                var roll = random.NextDouble();
                if (roll <= activity.Probabilities[0])
                {
                    duration += activity.Durations[0];
                }
                else if (roll <= activity.Probabilities[0] + activity.Probabilities[1])
                {
                    duration += activity.Durations[1];
                }
                else
                {
                    duration += activity.Durations[2];
                }
            }

            var existing = results.FirstOrDefault(r => r.Duration == duration);
            if (existing == null)
            {
                existing = new Result() { Duration = duration };
                results.Add(existing);
            }
            existing.Occurences++;
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

            Console.WriteLine("Assembling comprehensive list of activity & duration combinations...");

            var numDurations = activities[0].Durations.Length;

            //theoretically the number of possible outcomes
            var completeIterations = (long)Math.Round(Math.Pow(numDurations, activities.Length));

            var results = new List<List<int[]>>();

            var tasks = new List<Task<List<int[]>[]>>();
            var cores = 32;
            var jobSize = completeIterations / cores;

            long i = 0;
            while (i < completeIterations)
            {
                long endIndex;

                if (completeIterations - i < jobSize)
                {
                    endIndex = completeIterations;
                }
                else
                {
                    endIndex = i + jobSize;
                }

                tasks.Add(ThreadAssembleOperations(activities, i, endIndex, numDurations));

                i = endIndex;
            }

            foreach (var task in tasks)
            {
                results.AddRange(await task);
            }

            timer.Stop();

            Console.WriteLine("Assembled " + results.Count() + " different combinations (took " + timer.Elapsed.TotalSeconds + " seconds)");

            return results;
        }
        
        static async Task<List<int[]>[]> ThreadAssembleOperations(Activity[] activities, long iterationsStart, long iterationsStop, int numDurations)
        {
            //threadsafe somehow add processed results to 'results' variable,
            // since each thread will be running on a subset, they will all be accessing same results array, but different indexes
            return await Task.Run(() =>
            {
                var activityDurationIndexs = new int[activities.Length];
                var results = new List<int[]>[iterationsStop - iterationsStart];

                for (long i = iterationsStart; i < iterationsStop; i++)
                {
                    for (var j = 0; j < activityDurationIndexs.Length; j++)
                    {
                        if (i % (Math.Pow(numDurations, activities.Length - 1 - j)) == 0 && i != 0)
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
                    results[i - iterationsStart] = set;
                }

                return results;
            });
        }

        static void CalculateExpectedDuration(Activity[] activities, List<List<int[]>> results)
        {
            Console.WriteLine("Performing probability calculations on combinations...");

            var totalExpectedDuration = 0d;

            var l = 0;
            foreach (var operation in results)
            {
                l++;
                var subtotalDuration = 0d;
                double? subtotalExpectedProbability = null;
                var subtotalExpectedDuration = 0d;

                var i = 0;
                foreach (var value in operation)
                {
                    i++;
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

                totalExpectedDuration += subtotalExpectedDuration;
            }

            Console.WriteLine("Expected Duration: " + totalExpectedDuration);
            Console.WriteLine("DONE!\n");
        }
    }

    class Activity
    {
        public int ActivityNumber;

        public int[] Durations;
        public double[] Probabilities;
    }

    class Result
    {
        public double Duration;
        public int Occurences;
        public List<Activity> ActivitySet = new();
    }
}
