#include <iostream>
#include <ratio>
#include <chrono>

using namespace std;

double f(double x) {
    return 4.0 / (1 + x * x);
}

double integrate(int n, double a, double b) {
    double w = (b - a) / n;
    double sum = 0.0;

    for (int i = 0; i < n; i++) {
        sum += w * f(a + w * (i + 0.5));
    }
    return sum;

}

double integrateOMP(int n, double a, double b) {
    double w = (b - a) / n;
    double sum = 0.0;
#pragma omp parallel for reduction(+:sum)
        for (int i = 0; i < n; i++) {
            sum += w * f(a + w * (i + 0.5));
        }
    return sum;
}

int main() {
    int n = 1000000000;
    auto start = chrono::high_resolution_clock::now();
    double resSeq = integrate(n, 0.0, 1.0);
    auto end = chrono::high_resolution_clock::now();
    chrono::duration<double, std::milli> time = end - start;
    double timeSeq = time.count();


    start = chrono::high_resolution_clock::now();
    double resOMP = integrateOMP(n, 0.0, 1.0);
    end = chrono::high_resolution_clock::now();
    time = end - start;
    double timeOMP = time.count();

    cout << "n:         " << n << endl;
    cout << "Result:    " << resSeq << "(seq) " << resOMP << " (OMP)" << endl;
    cout << "Time:      " << timeSeq << "ms (seq) " << timeOMP << " ms (OMP" << endl;
    cout << "Speedup:   " <<  timeSeq / timeOMP << endl;
    cout << endl;

    return 0;
}
