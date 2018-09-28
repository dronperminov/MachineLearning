using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MachineLearning {
    public class KNearestNeighbors {
        public enum MetricType {
            Euclid,
            Manhattan
        }

        struct Data {
            public Vector vector;
            public int classIndex;
            public double distance;
        }

        delegate double Metric(Vector a, Vector b);

        int neighborsCount; // количество соседей, k
        int classesCount; // количество классов

        Data[] data;
        Metric metric;

        // Метрика Манхэттена (L1)
        public double ManhattanMetric(Vector a, Vector b) {
            if (a.Length != b.Length)
                throw new Exception("Euclid metric: vectors have different size");

            double distance = 0;

            for (int i = 0; i < a.Length; i++)
                distance += Math.Abs(a[i] - b[i]);

            return distance;
        }

        // Евклидова метрика (L2)
        public double EuclidMetric(Vector a, Vector b) {
            if (a.Length != b.Length)
                throw new Exception("Euclid metric: vectors have different size");

            double distance = 0;

            for (int i = 0; i < a.Length; i++)
                distance += (a[i] - b[i]) * (a[i] - b[i]);

            return Math.Sqrt(distance);
        }

        public KNearestNeighbors(Vector[] inputs, int[] classes, int classesCount, int k, MetricType type) {
            if (inputs.Length != classes.Length)
                throw new Exception("KNearesNeighbors: inputs and classes have different size");

            data = new Data[inputs.Length];

            for (int i = 0; i < data.Length; i++) {
                data[i].vector = inputs[i];
                data[i].classIndex = classes[i];
                data[i].distance = 0;
            }

            this.classesCount = classesCount;
            neighborsCount = k;

            if (type == MetricType.Euclid) {
                metric = EuclidMetric;
            }
            else if (type == MetricType.Manhattan) {
                metric = ManhattanMetric;
            }
            else {
                throw new Exception("Unknown metric!");
            }
        }

        // Классификация объекта (получение индекса класса)
        public int GetClassIndex(Vector input) {
            Parallel.For(0, data.Length, i => data[i].distance = metric(input, data[i].vector)); // считаем расстояния до всех векторов

            // находим k ближайших соседей
            for (int i = 0; i < neighborsCount; i++) {
                int imin = i;

                for (int j = 0; j < data.Length; j++)
                    if (data[j].distance < data[imin].distance)
                        imin = j;

                Data tmp = data[imin];
                data[imin] = data[i];
                data[i] = tmp;
            }

            // создаём массив для голосования
            double[] votes = new double[classesCount];

            // считаем голоса обратно пропорционально квадрату расстояния до объектов
            for (int i = 0; i < neighborsCount; i++)
                votes[data[i].classIndex] += 1.0 / (data[i].distance * data[i].distance);

            // ищем индекс класса с максимальным числом голосов
            int index = 0;

            for (int i = 1; i < votes.Length; i++)
                if (votes[i] > votes[index])
                    index = i;

            return index; // возвращаем индекс
        }
    }
}
