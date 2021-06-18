using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace ActivitySimulator
{
    class Program
    {
        static void Main(string[] args)
        {
            var activities = new Activity[]
            {
                new Activity() {
                    ActivityNumber = 0,
                    Durations = new int[] { 4, 8, 10 },
                    Probabilities = new double[] { 0.15, 0.50, 0.35 },
                },
                new Activity() {
                    ActivityNumber = 1,
                    Durations = new int[] { 1, 2, 4 },
                    Probabilities = new double[] { 0.25, 0.50, 0.25 },
                },
                new Activity() {
                    ActivityNumber = 0,
                    Durations = new int[] { 4, 8, 10 },
                    Probabilities = new double[] { 0.15, 0.50, 0.35 },
                },
                new Activity() {
                    ActivityNumber = 1,
                    Durations = new int[] { 1, 2, 4 },
                    Probabilities = new double[] { 0.25, 0.50, 0.25 },
                },
                new Activity() {
                    ActivityNumber = 0,
                    Durations = new int[] { 4, 8, 10 },
                    Probabilities = new double[] { 0.15, 0.50, 0.35 },
                },
                new Activity() {
                    ActivityNumber = 1,
                    Durations = new int[] { 1, 2, 4 },
                    Probabilities = new double[] { 0.25, 0.50, 0.25 },
                },
                new Activity() {
                    ActivityNumber = 0,
                    Durations = new int[] { 4, 8, 10 },
                    Probabilities = new double[] { 0.15, 0.50, 0.35 },
                },
                new Activity() {
                    ActivityNumber = 1,
                    Durations = new int[] { 1, 2, 4 },
                    Probabilities = new double[] { 0.25, 0.50, 0.25 },
                },
                new Activity() {
                    ActivityNumber = 0,
                    Durations = new int[] { 4, 8, 10 },
                    Probabilities = new double[] { 0.15, 0.50, 0.35 },
                },
                new Activity() {
                    ActivityNumber = 1,
                    Durations = new int[] { 1, 2, 4 },
                    Probabilities = new double[] { 0.25, 0.50, 0.25 },
                },
                new Activity() {
                    ActivityNumber = 0,
                    Durations = new int[] { 4, 8, 10 },
                    Probabilities = new double[] { 0.15, 0.50, 0.35 },
                },
                new Activity() {
                    ActivityNumber = 1,
                    Durations = new int[] { 1, 2, 4 },
                    Probabilities = new double[] { 0.25, 0.50, 0.25 },
                },
                new Activity() {
                    ActivityNumber = 1,
                    Durations = new int[] { 1, 2, 4 },
                    Probabilities = new double[] { 0.25, 0.50, 0.25 },
                },
            };

            //PerformTaskRandom(activities);

            PerformTaskNonrepeating(activities);
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
            var results = AssembleActivityDurationOperations(activities);

            CalculateExpectedDuration(activities, results);
        }

        static List<ProcessedActivitySet> AssembleActivityDurationOperations(Activity[] activities)
        {
            var timer = new Stopwatch();
            timer.Start();

            Console.WriteLine("Assembling comprehensive list of activity & duration combinations...");

            var numDurations = activities[0].Durations.Length;

            //theoretically the number of possible outcomes
            var completeIterations = Math.Pow(numDurations, activities.Length);

            var results = new List<ProcessedActivitySet>();

            var activityDurationIndexs = new int[activities.Length];

            for (var i = 0; i < completeIterations; i++)
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

                var set = new ProcessedActivitySet();
                for (var j = 0; j < activities.Count(); j++)
                {
                    var activity = new int[] { activities[j].ActivityNumber, activityDurationIndexs[j] };

                    set.Values.Add(activity);
                }
                results.Add(set);
            }

            timer.Stop();

            Console.WriteLine("Assembled " + results.Count() + " different combinations (took " + timer.Elapsed.TotalSeconds + " seconds)");

            return results;
        }

        static void CalculateExpectedDuration(Activity[] activities, List<ProcessedActivitySet> results)
        {
            Console.WriteLine("Performing probability calculations on combinations...");

            var totalExpectedDuration = 0d;

            foreach (var operation in results)
            {
                var subtotalDuration = 0d;
                double? subtotalExpectedProbability = null;
                var subtotalExpectedDuration = 0d;

                foreach (var value in operation.Values)
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

    class ProcessedActivitySet
    {
        //list of:
        // int[0] = activity number
        // int[1] index of chosen duration from durations array within the activity
        public List<int[]> Values = new List<int[]>();

        //public string Print()
        //{
        //    var sb = new StringBuilder();
        //    foreach (var value in Values)
        //    {
        //        sb.Append(value[0] + ":" + value[1] + " ");
        //    }

        //    return sb.ToString();
        //}
    }
}
