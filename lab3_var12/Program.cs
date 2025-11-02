/*
 * Написати програму для паралельного пошуку медіани варіаційного ряду
 * довжини n.
 */


using System.Diagnostics;

const int n = 5_000_000;
const int taskCount = 8;

var random = new Random();
var array = new double[n];
for (int i = 0; i < n; i++)
{
    array[i] = random.NextDouble() * 1000;
}

var watch = new Stopwatch();
watch.Start();
double medianSerial = FindMedianSerial(array);
watch.Stop();
var durationSerial = watch.Elapsed;

Console.WriteLine($"Serial median = {medianSerial:F3}, duration = {durationSerial}");

watch.Restart();
double medianParallel = FindMedianParallel(array, taskCount);
watch.Stop();
var durationParallel = watch.Elapsed;

double speedup = durationSerial.TotalMilliseconds / durationParallel.TotalMilliseconds;
Console.WriteLine($"Parallel median = {medianParallel:F3}, duration = {durationParallel}, speed-up = {speedup:F2}");

static double FindMedianSerial(double[] source)
{
    var data = (double[])source.Clone();
    Array.Sort(data);
    int n = data.Length;
    if (n % 2 == 1)
        return data[n / 2];
    else
        return (data[n / 2 - 1] + data[n / 2]) / 2.0;
}

static double FindMedianParallel(double[] source, int taskCount)
{
    int n = source.Length;
    var data = (double[])source.Clone();
    
    int blockSize = n / taskCount;
    int remainder = n % taskCount;
    int[] start = new int[taskCount];
    int[] length = new int[taskCount];
    int index = 0;
    for (int i = 0; i < taskCount; i++)
    {
        start[i] = index;
        length[i] = blockSize + (i < remainder ? 1 : 0);
        index += length[i];
    }
    
    var tasks = new Task[taskCount];
    for (int i = 0; i < taskCount; i++)
    {
        int from = start[i];
        int len = length[i];
        tasks[i] = Task.Factory.StartNew(() =>
        {
            Array.Sort(data, from, len);
        });
    }
    Task.WaitAll(tasks);
    
    int[] pos = new int[taskCount];
    int totalProcessed = -1;
    double val1 = 0, val2 = 0;

    int mid1 = (n - 1) / 2;
    int mid2 = n / 2;

    while (true)
    {
        double minVal = double.MaxValue;
        int minBlock = -1;

        for (int i = 0; i < taskCount; i++)
        {
            if (pos[i] >= length[i]) continue;
            double current = data[start[i] + pos[i]];
            if (current < minVal)
            {
                minVal = current;
                minBlock = i;
            }
        }

        if (minBlock == -1) break;

        pos[minBlock]++;
        totalProcessed++;

        if (totalProcessed == mid1) val1 = minVal;
        if (totalProcessed == mid2)
        {
            val2 = minVal;
            break;
        }
    }

    if (mid1 == mid2)
        return val1;
    else
        return (val1 + val2) / 2.0;
}
