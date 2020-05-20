using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace SubsetSum {
    internal static class SubsetSumA {
        static readonly ThreadLocal<Random> Random = new ThreadLocal<Random>(() => new Random());
        public static void Main(string[] args) {
            //0 - n, 1 - m (max element in array), 2 - A (a sum you wish to find)
            Execute(int.Parse(args[0]), int.Parse(args[1]), int.Parse(args[2]));
        }
        static void Execute(int n, int m, int A) {
            while (true)
            {
                var v = Generate(n, m);
                //var answer = SubsetSum(v, A);
                //Console.WriteLine($"Dynamic answer: {answer}");
                var (indices, sumIndices, sum) = Process(v, m, A);
                Console.WriteLine($"Idx:       {string.Join(" ", Enumerable.Range(0, indices[^1] + 1).Select(x => $"{x:00}"))}");
                Console.WriteLine($"Array      {string.Join(" ", v.Select(x => $"{x:00}"))}");
                Console.WriteLine($"Idx:       {string.Join(" ", Enumerable.Range(0, sumIndices.Length).Select(x => $"{x:00}"))}");
                Console.WriteLine($"Indices:   {string.Join(" ", indices.Select(x => $"{x:00}"))}");
                Console.WriteLine($"SumIndices:{string.Join(" ", sumIndices.Select(x => $"{x:00}"))}");
                var sequence = IterativeDownhillSisyphus(v, A, indices, sumIndices, sum);
                if (sequence == null)
                {
                    Console.WriteLine("Sequence not found!\n");
                    continue;
                }
                var ssum = sequence.Sum();
                Console.WriteLine($"Sequence:  {string.Join(" ", sequence)}, Sum: {ssum}\n");
                if (A != ssum) break;
                if (!CheckSequence(sequence, v[..(indices[^1] + 1)].ToList())) break;
            }
            Console.WriteLine("Error!");
            /*var watch = new Stopwatch();
            int max = n * 10, step = n, avg = 4;
            var arrs = new int[avg][];
            var writer = new StreamWriter("test7.csv", false);
            writer.WriteLine("n, Dynamic, PigeonholeSort, Sisyphus, Total");
            Console.Write($"\r0/{max / step}");
            for (var i = step; i <= max; i += step) {
                long s = 0L, r = 0L, t = 0L, d = 0L;
                Parallel.For(0, avg, j => arrs[j] = Generate(i, m));
                for (var j = 0; j < avg; ++j) {
                    watch.Restart();
                    SubsetSum(arrs[j], A);
                    watch.Stop();
                    Console.Write($"\r{i / step}/{max / step}, {j + 1}/{avg}, Dynamic");
                    watch.Restart();
                    var (indices, sumIndices, sum) = Process(arrs[j], m, A);
                    watch.Stop();
                    Console.Write($"\r{i / step}/{max / step}, {j + 1}/{avg}, Sort");
                    s += watch.ElapsedMilliseconds;
                    t += watch.ElapsedMilliseconds;
                    watch.Restart();
                    IterativeDownhillSisyphus(arrs[j], A, indices, sumIndices, sum);
                    watch.Stop();
                    Console.Write($"\r{i / step}/{max / step}, {j + 1}/{avg}, Iterative");
                    r += watch.ElapsedMilliseconds;
                    t += watch.ElapsedMilliseconds;
                }
                writer.WriteLine($"{i}, {s / (double) avg}, {r / (double) avg}, {t / (double) avg}, {d / (double)avg}, {r / (double)avg}");
            }
            writer.Close();*/
        }
        /// <summary>
        /// Basically Pigeonhole sort, that also ignores unnecessary elements in the array (such as 0s and elements larger than A)
        /// and constricts sorting to elements in range [1; min(m, A)] greatly reducing both linear time and space requirements.
        /// </summary>
        /// <param name="v">Array</param>
        /// <param name="m">Max element in array (or at least maximum of the generated array)</param>
        /// <param name="A">Target sum</param>
        /// <returns>
        /// indices which point to ceiling of indices[x] => v[i] <= x);
        /// sumIndices which point to last index of subarray starting from index one and constituting to at least sum x
        /// so that sumIndices[x] => sum(v[..(sumIndices[x] + 1)]) >= x;
        /// and sum of the whole sorted subarray.
        /// </returns>
        static (int[] indices, int[] sumIndices, int sum) Process(int[] v, int m, int A) {
            var t = Math.Min(m, A);
            var (indices, min) = (new int[t + 1], t + 1);
            foreach (var item in v) {
                if (item < 1 || item > t) continue;
                if (item < min) min = item;
                indices[item]++;
            }
            var (sumIndices, sum) = (new int[A + 1], 0);
            var it = 0;
            for (var i = min; i <= t; ++i) {
                var tCount = indices[i];
                indices[i] += indices[i - 1];
                for (var j = 0; j < tCount; ++j) {
                    v[it++] = i;
                    if (sum >= A) continue;
                    var tSum = sum + 1;
                    sum += i;
                    for (var k = tSum; k <= sum && k < sumIndices.Length; ++k)
                        sumIndices[k] = indices[i] - tCount + j;
                }
            }
            Array.Fill(sumIndices, -1, 0, min);
            for (var i = 0; i < indices.Length; ++i) indices[i]--;
            if (sum < A) sumIndices[A] = -1;
            return (indices, sumIndices, sum);
        }
        /// <summary>
        /// Gets index of ceiling of A in previously formed array of indices.
        /// </summary>
        /// <param name="A">Target sum</param>
        /// <param name="indices">Array of indices corresponding to ceilings of A found in v[]</param>
        /// <returns></returns>
        static int GetIndex(int A, int[] indices) => A >= indices.Length ? indices[^1] : indices[A];
        /// <summary>
        /// An algorithm that depending on both the array v[] and target sum attempts to find first correct subset
        /// by searching through constricted set of combinations defined by floor and ceiling.
        /// Each next element of combination is constrained between indices[nextSum] and sumIndices[nextSum],
        /// so as to not bother searching through combinations that will not result in wanted subset anyway.
        /// Operation does not cease until first combination of sum A is found or all the possibilities are explored.
        /// </summary>
        /// <param name="v">Sorted array of elements</param>
        /// <param name="A">Target sum</param>
        /// <param name="indices">Indices corresponding to array v[] formed by sorting procedure</param>
        /// <param name="sumIndices">SumIndices corresponding to array v[]</param>
        /// <param name="sum">The sum of the whole sorted subarray.</param>
        /// <returns>Returns a subset that constitutes to sum K or null if such subset is not found.</returns>
        static List<int> IterativeDownhillSisyphus(int[] v, int A, int[] indices, int[] sumIndices, int sum) {
            int f, c;
            //The whole sorted subarray does not constitute to wanted sum, no point in searching.
            if ((f = sumIndices[A]) == -1) return null;
            //The sum of subarray is exactly the target sum, also no point in searching;
            if (sum == A) return new List<int>(v[..(sumIndices[A] + 1)]); 
            //Ceiling of target sum does not exist, meaning any further search is futile. 
            if ((c = GetIndex(A, indices)) == -1) return null;
            //Check if ceiling index of A points to element that is equal to sum. 
            if(v[c] == A) return new List<int>(new[]{v[c]});
            //Basically the same as stack in recursion, except returns function arguments in FIFO sequence.
            var queue = new Queue<(int target, int floor, int ceil, List<int> path)>();
            queue.Enqueue((A, f, c, new List<int>()));
            while (queue.TryDequeue(out var item)) {
                var (target, floor, ceil, path) = item;
                //Any duplicate element will result in the same, but even more constrained possibility tree,
                //meaning it is not worth exploring again.
                var previous = -1;
                //Constrains possibilities between ceil and floor.
                for (var i = ceil; i >= floor; --i) {
                    if (v[i] == previous) continue;
                    //adds element to path (to be removed later)
                    path.Add(previous = v[i]);
                    //we decrease A depending on sum of subarray, until it the new target reaches 0.
                    var newTarget = target - v[i];
                    //if new subarray (path) constitutes to wanted sum, return it.
                    if (newTarget == 0) return path;
                    //find possibility constraints for new target sum, if either ceil or floor does not exist
                    //then subarray of new target sum is impossible.
                    int nFloor = sumIndices[newTarget], nCeil;
                    if (nFloor == -1 || (nCeil = Math.Min(i - 1, GetIndex(newTarget, indices))) == -1) {
                        path.RemoveAt(path.Count - 1);
                        continue;
                    }
                    //a smart check if sum(v[..(nFloor + 1)]) constitutes to the new target sum.
                    if (nFloor != sumIndices[newTarget + 1]) {
                        path.AddRange(v[..(nFloor + 1)]);
                        return path;
                    }
                    //check if element that is equal to target sum exists in array.
                    if (v[nCeil] == newTarget) {
                        path.Add(v[nCeil]);
                        return path;
                    }
                    //explore possibility tree further with new target sum, constraints and updated subarray.
                    queue.Enqueue((newTarget, nFloor, nCeil, new List<int>(path)));
                    //remove added element, so that the next possibility subtree evaluated correctly.
                    path.RemoveAt(path.Count - 1);
                }
            }
            return null;
        }
        /*static bool SubsetSum(int[] arr, int sum) {
            var subset = new bool[2, sum + 1];
            for (var i = 0; i <= arr.Length; ++i)
                for (var j = 0; j <= sum; ++j) {
                    if (j == 0) subset[i % 2, j] = true;
                    else if (i == 0) subset[i % 2, j] = false;
                    else if (arr[i - 1] <= j)
                        subset[i % 2, j] = subset[(i + 1) % 2, j - arr[i - 1]] || subset[(i + 1) % 2, j];
                    else subset[i % 2, j] = subset[(i + 1) % 2, j];
                }
            return subset[arr.Length % 2, sum];
        }*/
        //Checks if found sequence does not magically contain elements (including duplicates) that do not exist in starting array.
        static bool CheckSequence(IEnumerable<int> sequence, ICollection<int> arr) {
            return sequence.All(arr.Remove);
        }
        //Generates array of size n with maximum value being m.
        static int[] Generate(int n, int m) {
            return Enumerable.Range(0, n).Select(i => Random.Value.Next(0, m + 1)).ToArray();
        }
    }
}