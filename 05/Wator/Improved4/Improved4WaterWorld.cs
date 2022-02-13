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
        private const int MaxPartitionSize = 16;

        private readonly Random random;
        // A matrix of ints that determines the order of execution for each cell of the world.
        // This matrix is shuffled in each time step.
        // Cells of the world must be executed in a random order,
        // otherwise the animal in the first cell is always allowed to move first.
        private int[] randomMatrix;
        
        // for visualization
        private byte[] rgbValues;
        
        private readonly OrderablePartitioner<Tuple<int, int>> partitioner;

        #region Properties
        public int Width { get; private set; } // width (number of cells) of the world
        public int Height { get; private set; } // height (number of cells) of the world

        public Animal[]  Grid { get; private set; } // the cells of the world (2D-array of animal (fish or shark), empty cells have the value null)

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

            // populate the random matrix that determines the order of execution for the cells
            randomMatrix = GenerateMatrix(Width, Height, false);

            // Initialize the population by placing the required number of shark and fish
            // randomly on the grid.
            // randomMatrix contains all values from 0 .. Width*Height in a random ordering
            // so we can simply place a fish onto a cell if the value in the same cell of the
            // randomMatrix is smaller then the number of fish 
            // subsequently we can place a shark if the number in randomMatrix is smaller than
            // the number of fish and shark
            var initialMatrix = GenerateMatrix(Width, Height, true);
            for (var col = 0; col < Width; col++)
            {
                for (var row = 0; row < Height; row++)
                {
                    var value = initialMatrix[MapIndex(row, col)];
                    if (value < InitialFishPopulation)
                    {
                        Grid[MapIndex(row, col)] = new Fish(this, new Point(col, row), random.Next(0, FishBreedTime));
                    }
                    else if (value < InitialFishPopulation + InitialSharkPopulation)
                    {
                        Grid[MapIndex(row, col)] =
                            new Shark(this, new Point(col, row), random.Next(0, SharkBreedEnergy));
                    }
                    else
                    {
                        Grid[MapIndex(row, col)] = null;
                    }
                }
            }

            partitioner = Partitioner.Create(0, Height, MaxPartitionSize);
        }

        // Execute one time step of the simulation. Each cell of the world must be executed once.
        // Animals move around on the grid. To make sure each animal is executed only once we
        // use the moved flag.
        public void ExecuteStep()
        {
            Task.Run(async () =>
            {
                // First execute the even partitions then the odd so we do not produce race conditions
                ICollection<Task> tasks = new List<Task>();
                var partitions = partitioner.GetDynamicPartitions().ToArray();
                for (var i = 0; i < partitions.Length; i += 2)
                {
                    tasks.Add(ExecutePartAsync(partitions[i].Item1, partitions[i].Item2));
                }
                await Task.WhenAll(tasks);
                tasks.Clear();
                
                for (var i = 1; i < partitions.Length; i += 2)
                {
                    tasks.Add(ExecutePartAsync(partitions[i].Item1, partitions[i].Item2));
                }
                await Task.WhenAll(tasks);
                
                for (var col = 0; col < Width; col++)
                {
                    for (var row = 0; row < Height; row++)
                    {
                        Grid[MapIndex(row, col)]?.Commit();
                    }
                }
            }).Wait();
        }

        private Task ExecutePartAsync(int lower, int upper)
        {
            return Task.Run(() =>
            {
                RandomizeMatrix(randomMatrix, lower, upper);
                for (var col = 0; col < Width; col++)
                {
                    for (var row = lower; row < upper; row++)
                    {
                        var randomCol = randomMatrix[MapIndex(row, col)] % Width;
                        var randomRow = randomMatrix[MapIndex(row, col)] / Width;
                        var animal = Grid[MapIndex(randomRow, randomCol)];
                        if (animal != null && !animal.Moved)
                        {
                            animal.ExecuteStep();
                        }
                    }
                }
            });
        }

        // generate bitmap for the current state of the Wator world
        public Bitmap GenerateImage()
        {
            var counter = 0;
            for (var x = 0; x < Height; x++)
            {
                for (var y = 0; y < Width; y++)
                {
                    var col = Grid[MapIndex(x, y)] == null ? Color.DarkBlue : Grid[MapIndex(x, y)].Color;

                    rgbValues[counter++] = col.B; // blue
                    rgbValues[counter++] = col.G; // green
                    rgbValues[counter++] = col.R; // red
                    rgbValues[counter++] = col.A; // alpha
                }
            }

            var rect = new Rectangle(0, 0, Width, Height);
            var bitmap = new Bitmap(Width, Height);
            System.Drawing.Imaging.BitmapData bmpData = null;
            try
            {
                // lock the bitmap's bits
                bmpData = bitmap.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, bitmap.PixelFormat);

                // get the address of the first line
                var ptr = bmpData.Scan0;

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
        private List<Point> GetNeighbors(Type type, Point position)
        {
            //Point[] neighbors = new Point[4];
            var neighbors = new List<Point>();

            // look north
            var i = position.X;
            var j = (position.Y + Height - 1) % Height;
            var animal = Grid[MapIndex(j, i)];
            if (type == null && animal == null)
            {
                neighbors.Add(new Point(i, j));
            }
            else if (type != null && type.IsInstanceOfType(animal))
            {
                if (animal != null && !animal.Moved)
                {
                    // ignore animals moved in the current iteration
                    neighbors.Add(new Point(i, j));
                }
            }
            // look east
            i = (position.X + 1) % Width;
            j = position.Y;
            animal = Grid[MapIndex(j, i)];
            if (type == null && animal == null)
            {
                neighbors.Add(new Point(i, j));
            }
            else if (type != null && type.IsInstanceOfType(animal))
            {
                if (animal != null && !animal.Moved)
                {
                    neighbors.Add(new Point(i, j));
                }
            }
            // look south
            i = position.X;
            j = (position.Y + 1) % Height;
            animal = Grid[MapIndex(j, i)];
            if (type == null && animal == null)
            {
                neighbors.Add(new Point(i, j));
            }
            else if (type != null && type.IsInstanceOfType(animal))
            {
                if (animal != null && !animal.Moved)
                {
                    neighbors.Add(new Point(i, j));
                }
            }
            // look west
            i = (position.X + Width - 1) % Width;
            j = position.Y;
            animal = Grid[MapIndex(j, i)];
            if (type == null && animal == null)
            {
                neighbors.Add(new Point(i, j));
            }
            else if (type != null && type.IsInstanceOfType(animal))
            {
                if (animal != null && !animal.Moved)
                {
                    neighbors.Add(new Point(i, j));
                }
            }

            // create result array that only contains found cells
            //Point[] result = new Point[neighborIndex];
            //Array.Copy(neighbors, result, neighborIndex);
            return neighbors;
        }

        // select a random neighboring cell of the given position and type
        public Point SelectNeighbor(Type type, Point position)
        {
            IList<Point> neighbors = GetNeighbors(type, position); // find all neighbors of required type
            if (neighbors.Count > 1)
            {
                return neighbors[random.Next(neighbors.Count)]; // return random neighbor (prevent bias)
            }
            return neighbors.Count == 1 ? neighbors[0] : new Point(-1, -1);
        }

        // create a matrix containing all numbers from 0 to width * height in random order
        private int[] GenerateMatrix(int width, int height, bool randomize)
        {
            var matrix = new int[width * height];

            var row = 0;
            var col = 0;
            for (var i = 0; i < matrix.Length; i++)
            {
                matrix[MapIndex(row, col)] = i;
                col++;
                if (col < width) continue;
                col = 0;
                row++;
            }
            if (randomize)
            {
                RandomizeMatrix(matrix, 0, height); // shuffle
            }
            return matrix;
        }

        private void RandomizeMatrix(IList<int> matrix, int lower, int upper)
        {
            for (var i = lower * Width; i < upper * Width; i++)
            {
                var result = random.Next(i, upper * Width);
                var temp = matrix[result];
                matrix[result] = matrix[i];
                matrix[i] = temp;
            }
        }

        private int MapIndex(int row, int column)
        {
            return row * Width + column;
        }
    }
}
