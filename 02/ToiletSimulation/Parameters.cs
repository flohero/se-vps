namespace VPS.ToiletSimulation
{
  public static class Parameters
  {
    // number of producers
    public static int Producers => 2;

    // number of generated jobs per producer
    public static int JobsPerProducer => 50;

    // number of consumers
    public static int Consumers => 2;

    // if true, output of job processing is displayed
    public static bool DisplayJobProcessing => true;

    // mean time between arrival of new jobs (milliseconds)
    public static double MeanArrivalTime => 100;

    // mean of the time span in which a job has to be processed (milliseconds)
    public static int MeanDueTime => 500;

    // standard deviation of the time span in which a job has to be processed (milliseconds)
    public static int StdDeviationDueTime => 150;

    // mean of the time required to process a job (milliseconds)
    public static int MeanProcessingTime => 100;

    // standard deviation of the time required to process a job (milliseconds)
    public static int StdDeviationProcessingTime => 25;
  }
}