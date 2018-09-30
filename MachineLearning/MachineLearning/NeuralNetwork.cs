using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MachineLearning {
    public struct NeuroStructure {
        public int inputs; // число входных нейронов
        public int[] hiddens; // число скрытых нейронов
        public int outputs; // число выходных нейронов

        public ActivationType hiddensFunction; // тип функции активации в скрытых слоях
        public ActivationType outputFunction; // тип функции активации выходного слоя

        public string name; // имя сети
    }

    public struct TrainParameters {
        public double learningRate; // скорость обучения
        public double accuracy; // точность обучения
        public double dropOut; // доля исключаемых нейронов
        public long maxEpochs; // максимальное число эпох обучения
        public bool printTime; // печатать ли время эпохи

        public int autoSavePeriod; // интервал автосохранения сети в файл (0 - не сохранять)
    }

    public class NeuralNetwork {
        NeuroStructure structure; // структура сети

        Matrix[] weights; // слои (матрицы весов)

        ActivationFunction hiddensActivation; // функция активации скрытых слоёв
        ActivationFunction hiddensDerivative; // Производная функции активации скрытых слоёв
        ActivationFunction outputActivation; // функция активации выходного слоя
        ActivationFunction outputDerivative; // Производная функции активации выходного слоя

        public delegate void Log(double error, long epoch); // делегат для отслеживания состояния обучения

        public NeuralNetwork(NeuroStructure structure) {
            this.structure = structure;

            Create(); // создаём матрицы весов, входные и выходные сигналы


            // заполняем матрицу маленькими случайными числами
            for (int i = 0; i < weights.Length; i++)
                weights[i].SetRandom(0, Math.Sqrt(2.0 / this.weights[i].m));
        }

        // создание нейросети из файла, расположенного в path
        public NeuralNetwork(string path) {
            if (!File.Exists(path))
                throw new Exception("NeuralNetwork: fils does not exists");

            StreamReader reader = new StreamReader(path);

            structure.inputs = int.Parse(reader.ReadLine());
            structure.hiddens = new int[int.Parse(reader.ReadLine())];

            for (int i = 0; i < structure.hiddens.Length; i++)
                structure.hiddens[i] = int.Parse(reader.ReadLine());

            structure.outputs = int.Parse(reader.ReadLine());

            structure.hiddensFunction = (ActivationType)int.Parse(reader.ReadLine());
            structure.outputFunction = (ActivationType)int.Parse(reader.ReadLine());

            int index = path.LastIndexOf("\\");
            path = path.Substring(index + 1);
            index = path.LastIndexOf(".");
            structure.name = path.Substring(index + 1);

            Create(); // создаём матрицы весов, входные и выходные сигналы

            for (int layer = 0; layer < weights.Length; layer++) {
                for (int i = 0; i < weights[layer].n; i++) {
                    string row = reader.ReadLine();
                    string[] values = row.Split(' ');

                    for (int j = 0; j < weights[layer].m; j++)
                        weights[layer][i, j] = double.Parse(values[j]);
                }
            }

            reader.Close();
        }

        // создание матриц весов и входных и выходных сигналов
        void Create() {
            if (structure.inputs < 1)
                throw new Exception("Create NeuralNetwork: inputs must be greater than zero");

            if (structure.hiddens.Length == 0)
                throw new Exception("Create NeuralNetwork: hiddens is null or zero");

            for (int i = 0; i < structure.hiddens.Length; i++)
                if (structure.hiddens[i] < 1)
                    throw new Exception("Create NeuralNetwork: hiddens at " + i + " layer must be greater than zero");

            if (structure.outputs < 1)
                throw new Exception("Create NeuralNetwork: outputs must be greater than zero");

            weights = new Matrix[1 + structure.hiddens.Length];

            weights[0] = new Matrix(structure.hiddens[0], structure.inputs);

            for (int i = 0; i < structure.hiddens.Length - 1; i++)
                weights[i + 1] = new Matrix(structure.hiddens[i + 1], structure.hiddens[i]);

            weights[weights.Length - 1] = new Matrix(structure.outputs, structure.hiddens[structure.hiddens.Length - 1]);

            hiddensActivation = ActivationFunctions.GetFunction(structure.hiddensFunction);
            hiddensDerivative = ActivationFunctions.GetDerivative(structure.hiddensFunction);

            outputActivation = ActivationFunctions.GetFunction(structure.outputFunction);
            outputDerivative = ActivationFunctions.GetDerivative(structure.outputFunction);
        }

        // получение структуры нейросети
        public NeuroStructure GetStructure() {
            return structure;
        }

        // получение выхода сети для вектора input
        public Vector GetOutput(Vector input) {
            Vector output;

            // распространяем сигнал от начала к концу
            for (int i = 0; i < weights.Length - 1; i++) {
                output = weights[i] * input;
                input = output.Activate(hiddensActivation);
            }

            output = weights[weights.Length - 1] * input;

            return output.Activate(outputActivation); // возвращаем активированный вектор
        }

        // сохранение значений весов из локальной переменной
        void SaveWeights(Matrix[] weights) {
            for (int layer = 0; layer < weights.Length; layer++) {
                for (int i = 0; i < weights[layer].n; i++) {
                    for (int j = 0; j < weights[layer].m; j++)
                        this.weights[layer][i, j] = weights[layer][i, j];
                }
            }
        }

        string PrettyTime(TimeSpan time) {
            if (time.TotalMilliseconds < 1000)
                return time.ToString("fff") + " ms";

            if (time.TotalMilliseconds < 60 * 1000)
                return time.ToString(@"ss\.fff") + "s";

            if (time.TotalMilliseconds < 60 * 60 * 1000)
                return time.ToString(@"mm\:ss\.fff");

            return time.ToString(@"hh\:mm\:ss\:fff");
        }

        // обучение сети методом обратного распространения ошибки с моментом
        public void TrainMoment(Vector[] inputData, Vector[] outputData, TrainParameters parameters, double moment, Log log = null) {
            Matrix[] weights = new Matrix[1 + structure.hiddens.Length];
            Matrix[] dw = new Matrix[weights.Length];

            weights[0] = new Matrix(structure.hiddens[0], structure.inputs);

            for (int i = 0; i < structure.hiddens.Length - 1; i++)
                weights[i + 1] = new Matrix(structure.hiddens[i + 1], structure.hiddens[i]);

            weights[weights.Length - 1] = new Matrix(structure.outputs, structure.hiddens[structure.hiddens.Length - 1]);

            for (int layer = 0; layer < weights.Length; layer++) {
                dw[layer] = new Matrix(weights[layer].n, weights[layer].m);

                for (int i = 0; i < weights[layer].n; i++) {
                    for (int j = 0; j < weights[layer].m; j++)
                        weights[layer][i, j] = this.weights[layer][i, j];
                }
            }

            Vector[] inputs = new Vector[weights.Length];
            Vector[] outputs = new Vector[weights.Length];
            Vector[] gradients = new Vector[weights.Length];
            Vector[] errors = new Vector[weights.Length];

            errors[weights.Length - 1] = new Vector(structure.outputs);

            long epoch = 0;
            double error;
            double minErrorr = double.PositiveInfinity;

            int last = weights.Length - 1;

            Stopwatch t = new Stopwatch();
            Stopwatch total = new Stopwatch();

            do {
                error = 0;
                t.Restart();
                total.Start();

                for (int index = 0; index < inputData.Length; index++) {
                    inputs[0] = inputData[index];

                    // распространяем сигнал от начала к концу
                    for (int i = 0; i < last; i++) {
                        outputs[i] = weights[i] * inputs[i];
                        inputs[i + 1] = outputs[i].Activate(hiddensActivation);
                        gradients[i] = outputs[i].Activate(hiddensDerivative);
                    }

                    outputs[last] = weights[last] * inputs[last];
                    gradients[last] = outputs[last].Activate(outputDerivative);

                    // считаем ошибку выхода сети
                    for (int i = 0; i < structure.outputs; i++) {
                        double e = outputData[index][i] - outputActivation(outputs[last][i]); // компонента ошибки
                        errors[last][i] = e;

                        error += e * e; // добавляем квадрат ошибки к общей ошибке
                    }

                    // распространяем ошибку выше
                    for (int i = last; i > 0; i--)
                        errors[i - 1] = weights[i] ^ errors[i];

                    // изменяем веса в каждом слое
                    for (int layer = 0; layer < weights.Length; layer++) {
                        for (int i = 0; i < weights[layer].n; i++) {
                            double delta = parameters.learningRate * errors[layer][i] * gradients[layer][i];

                            for (int j = 0; j < weights[layer].m; j++) {
                                double deltaW = delta * inputs[layer][j];

                                weights[layer][i, j] += deltaW + moment * dw[layer][i, j];
                                dw[layer][i, j] = deltaW;
                            }
                        }
                    }
                }

                error = Math.Sqrt(error);
                epoch++;

                t.Stop();
                total.Stop();

                if (parameters.printTime) {
                    Console.ForegroundColor = minErrorr < error ? ConsoleColor.Red : ConsoleColor.Gray;
                    Console.WriteLine("epoch time: {0}, total time: {1}", PrettyTime(t.Elapsed), PrettyTime(total.Elapsed));
                }

                if (log != null)
                    log(error, epoch);

                if (parameters.autoSavePeriod > 0 && epoch % parameters.autoSavePeriod == 0) {
                    SaveWeights(weights);
                    Save(structure.name + "_" + epoch + "_" + error + ".txt");
                }
            } while (error > parameters.accuracy && epoch < parameters.maxEpochs); // повторяем пока не достигнем нужной точности или максимального числа эпох

            // сохраняем веса в поле класса
            SaveWeights(weights);

            if (epoch == parameters.maxEpochs)
                Console.WriteLine("Warning! Max epoch reached!");
        }

        // обучение сети методом обратного распространения ошибки
        public void Train(Vector[] inputData, Vector[] outputData, TrainParameters parameters, Log log = null) {
            Matrix[] weights = new Matrix[1 + structure.hiddens.Length];

            weights[0] = new Matrix(structure.hiddens[0], structure.inputs);

            for (int i = 0; i < structure.hiddens.Length - 1; i++)
                weights[i + 1] = new Matrix(structure.hiddens[i + 1], structure.hiddens[i]);

            weights[weights.Length - 1] = new Matrix(structure.outputs, structure.hiddens[structure.hiddens.Length - 1]);

            for (int layer = 0; layer < weights.Length; layer++) {
                for (int i = 0; i < weights[layer].n; i++) {
                    for (int j = 0; j < weights[layer].m; j++)
                        weights[layer][i, j] = this.weights[layer][i, j];
                }
            }

            Vector[] inputs = new Vector[weights.Length];
            Vector[] outputs = new Vector[weights.Length];
            Vector[] gradients = new Vector[weights.Length];
            Vector[] errors = new Vector[weights.Length];

            errors[weights.Length - 1] = new Vector(structure.outputs);

            int last = weights.Length - 1;
            long epoch = 0;
            double error;
            double minErrorr = double.PositiveInfinity;

            Stopwatch t = new Stopwatch();
            Stopwatch total = new Stopwatch();

            do {
                error = 0;
                t.Restart();
                total.Start();

                for (int index = 0; index < inputData.Length; index++) {
                    inputs[0] = inputData[index];

                    // распространяем сигнал от начала к концу
                    for (int i = 0; i < last; i++) {
                        outputs[i] = weights[i] * inputs[i];
                        inputs[i + 1] = outputs[i].Activate(hiddensActivation);
                        gradients[i] = outputs[i].Activate(hiddensDerivative);
                    }

                    outputs[last] = weights[last] * inputs[last];
                    gradients[last] = outputs[last].Activate(outputDerivative);

                    // считаем ошибку выхода сети
                    for (int i = 0; i < structure.outputs; i++) {
                        double e = outputData[index][i] - outputActivation(outputs[last][i]); // компонента ошибки
                        errors[last][i] = e;

                        error += e * e; // добавляем квадрат ошибки к общей ошибке
                    }

                    // распространяем ошибку выше
                    for (int i = last; i > 0; i--)
                        errors[i - 1] = weights[i] ^ errors[i];

                    // изменяем веса в каждом слое
                    for (int layer = 0; layer < weights.Length; layer++) {
                        Parallel.For(0, weights[layer].n, i => {
                            double delta = parameters.learningRate * errors[layer][i] * gradients[layer][i];

                            for (int j = 0; j < weights[layer].m; j++) {
                                weights[layer][i, j] += delta * inputs[layer][j];
                            }
                        });
                    }
                }

                error = Math.Sqrt(error);

                if (error < minErrorr)
                    minErrorr = error;

                epoch++;

                t.Stop();
                total.Stop();

                if (parameters.printTime) {
                    Console.ForegroundColor = minErrorr < error ? ConsoleColor.Red : ConsoleColor.Gray;
                    Console.WriteLine("epoch time: {0}, total time: {1}", PrettyTime(t.Elapsed), PrettyTime(total.Elapsed));
                }

                if (log != null)
                    log(error, epoch);

                if (parameters.autoSavePeriod > 0 && epoch % parameters.autoSavePeriod == 0) {
                    SaveWeights(weights);
                    Save(structure.name + "_" + epoch + "_" + error + ".txt");
                }
            } while (error > parameters.accuracy && epoch < parameters.maxEpochs); // повторяем пока не достигнем нужной точности или максимального числа эпох

            // сохраняем веса в поле класса
            SaveWeights(weights);

            if (epoch == parameters.maxEpochs)
                Console.WriteLine("Warning! Max epoch reached!");
        }

        // сохранение сети в файл, находящийся в path
        public void Save(string path) {
            StreamWriter writer = new StreamWriter(path);

            writer.WriteLine(structure.inputs);

            writer.WriteLine(structure.hiddens.Length);

            for (int i = 0; i < structure.hiddens.Length; i++)
                writer.WriteLine(structure.hiddens[i]);

            writer.WriteLine(structure.outputs);

            writer.WriteLine((int)structure.hiddensFunction);
            writer.WriteLine((int)structure.outputFunction);

            for (int layer = 0; layer < weights.Length; layer++) {
                for (int i = 0; i < weights[layer].n; i++) {
                    for (int j = 0; j < weights[layer].m; j++)
                        writer.Write("{0} ", weights[layer][i, j]);

                    writer.WriteLine();
                }
            }

            writer.Close();
        }
    }
}
