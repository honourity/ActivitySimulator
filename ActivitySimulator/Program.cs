﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActivitySimulator
{
    class Program
    {
        static string inputFileName = "input.csv";
        static string outputFileName = "output.csv";

        static void Main(string[] args)
        {
            //13 activities takes about 6 seconds non-threaded (14 activities takes 30 seconds)
            //13 activities takes about 2 seconds threaded (14 activities takes 6.5 seconds)

            var activities = LoadActivities();

            //double iterations = 1000000;
            //PerformTaskRandom(activities, iterations);

            PerformTaskNonrepeating(activities);
        }

        static Activity[] LoadActivities()
        {
            var fileContents = File.ReadAllLines(inputFileName);
            var rows = fileContents.Where(f => !string.IsNullOrWhiteSpace(f)).ToList();
            rows.RemoveAt(0);
            //skip header row, reduce by 1, start at 1 for loop
            var activities = new Activity[rows.Count() - 1];

            for (var row = 1; row < rows.Count(); row++)
            {
                var line = rows[row];
                var split = line.Split(',');
                activities[row - 1] = new Activity()
                {
                    ActivityNumber = int.Parse(split[0]),
                    Durations = new decimal[] { int.Parse(split[1]), int.Parse(split[2]), int.Parse(split[3]) },
                    Probabilities = new decimal[] { decimal.Parse(split[4]), decimal.Parse(split[5]), decimal.Parse(split[6]) }
                };
            }

            Console.WriteLine("Loaded " + activities.Length + " activities from " + inputFileName);

            return activities;
        }

        static void PerformTaskRandom(Activity[] activities, int iterations)
        {
            Console.WriteLine("Performing random probablistic duration calculations with " + iterations + " iterations");

            var random = new Random();
            var results = new List<Result>();
            
            for (int i = 0; i < iterations; i++) { PerformTaskRandomIteration(activities, results, random); }

            var ordered = results.OrderBy(r => r.Duration);
            decimal expectedDuration = 0;
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
            decimal duration = 0;
            foreach (var activity in activities)
            {
                //add up probabilities and compare with random roll, a bit like a pie chart
                var roll = (decimal)random.NextDouble();
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

            Console.WriteLine("Assembling full list of activity & duration combinations...");

            var numDurations = activities[0].Durations.Length;

            //theoretically the number of possible outcomes
            var completeIterations = (ulong)Math.Round(Math.Pow(numDurations, activities.Length));

            var results = new List<List<int[]>>();

            var tasks = new List<Task<List<List<int[]>>>>();
            var cores = (ulong)Environment.ProcessorCount;
            var jobSize = completeIterations / cores;

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

                tasks.Add(ThreadAssembleOperations(activities, i, endIndex, numDurations));

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
        
        static async Task<List<List<int[]>>> ThreadAssembleOperations(Activity[] activities, ulong iterationsStart, ulong iterationsStop, int numDurations)
        {
            return await Task.Run(() =>
            {
                var activityDurationIndexs = new int[activities.Length];
                var results = new List<List<int[]>>();
                var activityDurationIndexesLength = (ulong)activityDurationIndexs.Length;
                var activitiesLength = (ulong)activities.Length;

                for (ulong i = iterationsStart; i < iterationsStop; i++)
                {
                    for (ulong j = 0; j < activityDurationIndexesLength; j++)
                    {
                        if (i % (Math.Pow(numDurations, activitiesLength - 1 - j)) == 0 && i != 0)
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

            //setting up csv header row
            var output = new StringBuilder();
            output.Append("Scenario Combinations,");
            foreach (var activity in activities)
            {
                output.Append("D(");
                output.Append(activity.ActivityNumber);
                output.Append("),");
            }
            output.Append("Total Duration,");
            foreach (var activity in activities)
            {
                output.Append("P(");
                output.Append(activity.ActivityNumber);
                output.Append("),");
            }
            output.Append("Expected Probability,Expected Duration\n");

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
                    output.Append(value[1]);
                }
                output.Append(',');
                foreach (var value in operation)
                {
                    output.Append(activities[value[0]].Durations[value[1]]);
                    output.Append(',');
                }
                output.Append(subtotalDuration);
                output.Append(',');
                foreach (var value in operation)
                {
                    output.Append(activities[value[0]].Probabilities[value[1]]);
                    output.Append(',');
                }
                if (subtotalExpectedProbability != null) output.Append(subtotalExpectedProbability.Value.ToString("0.############################"));
                output.Append(',');
                output.Append(subtotalExpectedDuration.ToString("0.############################"));
                output.AppendLine();

                totalExpectedDuration += subtotalExpectedDuration;
            }

            File.WriteAllText(outputFileName, output.ToString());

            Console.WriteLine("Calculations written to " + outputFileName);

            Console.WriteLine("Expected Duration: " + totalExpectedDuration);

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
