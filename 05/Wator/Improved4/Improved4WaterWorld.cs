using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace VPS.Wator.Improved4
{
    // initial object-oriented implementation of the Wator world simulation
    public class Improved4WatorWorld : IWatorWorld
    {
        private Random random;

        // for visualization
        private byte[] rgbValues;

        private int[] randomMatrix;

        private readonly OrderablePartitioner<Tuple<int, int>> partitioner;

        #region Properties
        public int Width { get; private set; }  // width (number of cells) of the world
        public int Height { get; private set; }  // height (number of cells) of the world
        public Animal[] Grid { get; private set; }  // the cells of the world (2D-array of animal (fish or shark), empty cells have the value null)

        // simulation parameters
        public int InitialFishPopulation { get; private set; }
        public int InitialFishEnergy { get; private set; }
        public int FishBreedTime { get; private set; }

        public int InitialSharkPopulation { get; private set; }
        public int InitialSharkEnergy { get; private set; }
        public int SharkBreedEnergy { get; private set; }
        #endregion

        public Improved4WatorWorld(Settings settings)
        {
            Width = settings.Width;
            Height = settings.Height;
            InitialFishPopulation = settings.InitialFishPopulation;
            InitialFishEnergy = settings.InitialFishEnergy;
            FishBreedTime = settings.FishBreedTime;
            InitialSharkPopulation = settings.InitialSharkPopulation;
            InitialSharkEnergy = settings.InitialSharkEnergy;
            SharkBreedEnergy = settings.SharkBreedEnergy;

            rgbValues = new byte[Width * Height * 4];

            random = new Random();
            Grid = new Animal[Width * Height];

            // Don't randomize matrix initially
            randomMatrix = GenerateMatrix(Width, Height, false);


            // Randomize positions
            var initialMatrix = GenerateMatrix(Width, Height, true);
            for (int col = 0; col < Width; col++)
            {
                for (int row = 0; row < Height; row++)
                {
                    int value = initialMatrix[MapIndex(row, col)];
                    if (value < InitialFishPopulation)
                    {
                        Grid[MapIndex(row, col)] = new Fish(this, new Point(col, row), random.Next(0, FishBreedTime));
                    }
                    else if (value < InitialFishPopulation + InitialSharkPopulation)
                    {
                        Grid[MapIndex(row, col)] = new Shark(this, new Point(col, row), random.Next(0, SharkBreedEnergy));
                    }
                    else
                    {
                        Grid[MapIndex(row, col)] = null;
                    }
                }
            }

            for (int partitionSize = 4; partitionSize < Height; partitionSize++)
            {
                partitioner = Partitioner.Create(0, Height, partitionSize);
                if (partitioner.GetDynamicPartitions().ToList().Count() % 2 == 0)
                {
                    break;
                }
            }
        }

        public void ExecuteStep()
        {
            Task.Run(ExecuteStepAsync).Wait();
        }

        private Task ExecutePartitionAsync(int lowerHeight, int upperHeight)
        {
            return Task.Run(() =>
            {
                RandomizeMatrix(randomMatrix, lowerHeight, upperHeight);

                int randomRow, randomCol;
                for (int col = 0; col < Width; col++)
                {
                    for (int row = lowerHeight; row < upperHeight; row++)
                    {
                        randomCol = randomMatrix[MapIndex(row, col)] % Width;
                        randomRow = randomMatrix[MapIndex(row, col)] / Width;

                        var animal = Grid[MapIndex(randomRow, randomCol)];

                        if (animal != null && !animal.Moved)
                        {
                            animal.ExecuteStep();
                        }
                    }
                }
            });
        }

        private async Task ExecuteStepAsync()
        {
            var partitions = partitioner.GetDynamicPartitions().ToList();
            ICollection<Task> tasks = new List<Task>();

            // Execute even partitions
            for (int i = 0; i < partitions.Count; i += 2)
            {
                tasks.Add(ExecutePartitionAsync(partitions[i].Item1, partitions[i].Item2));
            }

            await Task.WhenAll(tasks);
            tasks.Clear();

            // Execute uneven partitions
            for (int i = 1; i < partitions.Count; i += 2)
            {
                tasks.Add(ExecutePartitionAsync(partitions[i].Item1, partitions[i].Item2));
            }

            await Task.WhenAll(tasks);

            // Commit all animals in the grid to prepare for the next simulation step
            for (int col = 0; col < Width; col++)
            {
                for (int row = 0; row < Height; row++)
                {
                    Grid[MapIndex(row, col)]?.Commit();
                }
            }
        }

        // generate bitmap for the current state of the Wator world
        public Bitmap GenerateImage()
        {
            int counter = 0;
            for (int row = 0; row < Height; row++)
            {
                for (int col = 0; col < Width; col++)
                {
                    Color color;
                    if (Grid[MapIndex(row, col)] == null) color = Color.DarkBlue;
                    else color = Grid[MapIndex(row, col)].Color;

                    rgbValues[counter++] = color.B; // blue
                    rgbValues[counter++] = color.G; // green
                    rgbValues[counter++] = color.R; // red
                    rgbValues[counter++] = color.A; // alpha
                }
            }

            Rectangle rect = new Rectangle(0, 0, Width, Height);
            var bitmap = new Bitmap(Width, Height);
            System.Drawing.Imaging.BitmapData bmpData = null;
            try
            {
                // lock the bitmap's bits
                bmpData = bitmap.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, bitmap.PixelFormat);

                // get the address of the first line
                IntPtr ptr = bmpData.Scan0;

                // copy RGB values back to the bitmap
                System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, rgbValues.Length);
            }
            finally
            {
                // unlock the bits
                if (bmpData != null) bitmap.UnlockBits(bmpData);
            }
            return bitmap;
        }

        // find all neighboring cells of the given position and type
        public List<Point> GetNeighbors(Type type, Point position)
        {
            // neighbour points
            var points = new List<Point>();

            // look north
            var i = position.X;
            var j = (position.Y + Height - 1) % Height;
            var animal = Grid[MapIndex(j, i)];
            if (type == null && animal == null)
            {
                points.Add(new Point(i, j));
            }
            else if (type != null && type.IsInstanceOfType(animal))
            {
                if (animal != null && !animal.Moved)
                {  // ignore animals moved in the current iteration
                    points.Add(new Point(i, j));
                }
            }
            // look east
            i = (position.X + 1) % Width;
            j = position.Y;
            animal = Grid[MapIndex(j, i)];
            if (type == null && animal == null)
            {
                points.Add(new Point(i, j));
            }
            else if (type != null && type.IsInstanceOfType(animal))
            {
                if (animal != null && !animal.Moved)
                {
                    points.Add(new Point(i, j));
                }
            }
            // look south
            i = position.X;
            j = (position.Y + 1) % Height;
            animal = Grid[MapIndex(j, i)];
            if (type == null && animal == null)
            {
                points.Add(new Point(i, j));
            }
            else if (type != null && type.IsInstanceOfType(animal))
            {
                if (animal != null && !animal.Moved)
                {
                    points.Add(new Point(i, j));
                }
            }
            // look west
            i = (position.X + Width - 1) % Width;
            j = position.Y;
            animal = Grid[MapIndex(j, i)];
            if (type == null && animal == null)
            {
                points.Add(new Point(i, j));
            }
            else if (type != null && type.IsInstanceOfType(animal))
            {
                if (animal != null && !animal.Moved)
                {
                    points.Add(new Point(i, j));
                }
            }

            return points;
        }

        // select a random neighboring cell of the given position and type
        public Point SelectNeighbor(Type type, Point position)
        {
            IList<Point> neighbors = GetNeighbors(type, position);  // find all neighbors of required type
            if (neighbors.Count > 1)
            {
                return neighbors[random.Next(neighbors.Count)];  // return random neighbor (prevent bias)
            }
            else if (neighbors.Count == 1)
            {  // only one neighbor -> return without calling random
                return neighbors[0];
            }
            else
            {
                return new Point(-1, -1);  // no neighbor found
            }
        }

        // create a matrix containing all numbers from 0 to width * height in random order
        private int[] GenerateMatrix(int width, int height, bool randomize)
        {
            int[] matrix = new int[width * height];

            int row = 0;
            int col = 0;
            for (int i = 0; i < matrix.Length; i++)
            {
                matrix[MapIndex(row, col)] = i;
                col++;
                if (col >= width) { col = 0; row++; }
            }
            if (randomize)
            {
                RandomizeMatrix(matrix, 0, height);  // shuffle
            }

            return matrix;
        }

        private void RandomizeMatrix(int[] array, int lower, int upper)
        {
            for (int i = lower * Width; i < upper * Width; i++)
            {
                int result = random.Next(i, Width * upper);
                int temp = array[result];
                array[result] = array[i];
                array[i] = temp;
            }
        }

        public int MapIndex(int row, int column)
        {
            return row * Width + column;
        }
    }
}
