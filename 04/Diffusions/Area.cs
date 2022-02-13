namespace Diffusions {
  public class Area {
    public int Width { get; set; }
    public int Height { get; set; }
    public double[,] Matrix { get; set; }
    public double[,] NextMatrix { get; set; }

    public Area(int width, int height) {
      Matrix = new double[width, height];
      NextMatrix = new double[width, height];
      Width = width;
      Height = height;
    }
  }
}
