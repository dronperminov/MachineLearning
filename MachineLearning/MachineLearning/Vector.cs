using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MachineLearning {
    class Vector {
        public readonly int Length; // длина вектора
        public double[] values; // значения вектора

        public Vector(int n) {
            Length = n;
            values = new double[Length];
        }

        public Vector(double[] array) {
            if (array == null || array.Length == 0)
                throw new Exception("Vector(double[]): array is null or empty");

            Length = array.Length;

            values = new double[Length];

            for (int i = 0; i < Length; i++)
                values[i] = array[i];
        }

        public double this[int i] {
            get { return values[i]; }
            set { values[i] = value; }
        }

        // вывод вектора на консоль
        public void Print() {
            for (int i = 0; i < Length; i++)
                Console.Write("{0}  ", values[i]);

            Console.WriteLine();
        }

        // копирование вектора
        public Vector Copy() {
            return new Vector(values);
        }
    }
}
