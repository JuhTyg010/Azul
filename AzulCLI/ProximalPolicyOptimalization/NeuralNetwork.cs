namespace PPO {
    class NeuralNetWork {
        private Random _radomObj;

        public NeuralNetWork(int synapseMatrixColumns, int synapseMatrixLines) {
            SynapseMatrixColumns = synapseMatrixColumns;
            SynapseMatrixLines = synapseMatrixLines;

            _Init();
        }

        public int SynapseMatrixColumns { get; }
        public int SynapseMatrixLines { get; }
        public double[,] SynapsesMatrix { get; private set; }

        private void _Init() {
            _radomObj = new Random();
            _GenerateSynapsesMatrix();
        }

        private void _GenerateSynapsesMatrix() {
            SynapsesMatrix = new double[SynapseMatrixLines, SynapseMatrixColumns];

            for (var i = 0; i < SynapseMatrixLines; i++) {
                for (var j = 0; j < SynapseMatrixColumns; j++) {
                    SynapsesMatrix[i, j] = (2 * _radomObj.NextDouble()) - 1;
                }
            }
        }

        private double[,] _CalculateSigmoid(double[,] matrix) {
            int rowLength = matrix.GetLength(0);
            int colLength = matrix.GetLength(1);

            for (int i = 0; i < rowLength; i++) {
                for (int j = 0; j < colLength; j++) {
                    var value = matrix[i, j];
                    matrix[i, j] = 1 / (1 + Math.Exp(value * -1));
                }
            }
            return matrix;
        }

        private double[,] _CalculateSigmoidDerivative(double[,] matrix) {
            int rowLength = matrix.GetLength(0);
            int colLength = matrix.GetLength(1);

            for (int i = 0; i < rowLength; i++) {
                for (int j = 0; j < colLength; j++) {
                    var value = matrix[i, j];
                    matrix[i, j] = value * (1 - value);
                }
            }
            return matrix;
        }

        public double[,] Think(double[,] inputMatrix) {
            var productOfTheInputsAndWeights = MatrixDotProduct(inputMatrix, SynapsesMatrix);
            return _CalculateSigmoid(productOfTheInputsAndWeights);
        }

        public void Train(double[,] trainInputMatrix, double[,] trainOutputMatrix, int interactions) {
            for (var i = 0; i < interactions; i++) {
                var output = Think(trainInputMatrix);

                var error = MatrixSubstract(trainOutputMatrix, output);
                var curSigmoidDerivative = _CalculateSigmoidDerivative(output);
                var error_SigmoidDerivative = MatrixProduct(error, curSigmoidDerivative);

                var adjustment = MatrixDotProduct(MatrixTranspose(trainInputMatrix), error_SigmoidDerivative);

                SynapsesMatrix = MatrixSum(SynapsesMatrix, adjustment);
            }
        }

        // Matrix operations below

        private double[,] MatrixDotProduct(double[,] matrixA, double[,] matrixB) {
            int rowsA = matrixA.GetLength(0);
            int colsA = matrixA.GetLength(1);
            int rowsB = matrixB.GetLength(0);
            int colsB = matrixB.GetLength(1);

            if (colsA != rowsB) {
                throw new InvalidOperationException("Matrix dimensions must match for multiplication.");
            }

            double[,] result = new double[rowsA, colsB];

            for (int i = 0; i < rowsA; i++) {
                for (int j = 0; j < colsB; j++) {
                    result[i, j] = 0;
                    for (int k = 0; k < colsA; k++) {
                        result[i, j] += matrixA[i, k] * matrixB[k, j];
                    }
                }
            }

            return result;
        }

        private double[,] MatrixSubstract(double[,] matrixA, double[,] matrixB) {
            int rowsA = matrixA.GetLength(0);
            int colsA = matrixA.GetLength(1);
            int rowsB = matrixB.GetLength(0);
            int colsB = matrixB.GetLength(1);

            if (rowsA != rowsB || colsA != colsB) {
                throw new InvalidOperationException("Matrices must have the same dimensions for subtraction.");
            }

            double[,] result = new double[rowsA, colsA];

            for (int i = 0; i < rowsA; i++) {
                for (int j = 0; j < colsA; j++) {
                    result[i, j] = matrixA[i, j] - matrixB[i, j];
                }
            }

            return result;
        }

        private double[,] MatrixProduct(double[,] matrixA, double[,] matrixB) {
            int rowsA = matrixA.GetLength(0);
            int colsA = matrixA.GetLength(1);
            int rowsB = matrixB.GetLength(0);
            int colsB = matrixB.GetLength(1);

            if (rowsA != rowsB || colsA != colsB) {
                throw new InvalidOperationException("Matrices must have the same dimensions for element-wise multiplication.");
            }

            double[,] result = new double[rowsA, colsA];

            for (int i = 0; i < rowsA; i++) {
                for (int j = 0; j < colsA; j++) {
                    result[i, j] = matrixA[i, j] * matrixB[i, j];
                }
            }

            return result;
        }

        private double[,] MatrixTranspose(double[,] matrix) {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);

            double[,] result = new double[cols, rows];

            for (int i = 0; i < rows; i++) {
                for (int j = 0; j < cols; j++) {
                    result[j, i] = matrix[i, j];
                }
            }

            return result;
        }

        private double[,] MatrixSum(double[,] matrixA, double[,] matrixB) {
            int rowsA = matrixA.GetLength(0);
            int colsA = matrixA.GetLength(1);
            int rowsB = matrixB.GetLength(0);
            int colsB = matrixB.GetLength(1);

            if (rowsA != rowsB || colsA != colsB) {
                throw new InvalidOperationException("Matrices must have the same dimensions for addition.");
            }

            double[,] result = new double[rowsA, colsA];

            for (int i = 0; i < rowsA; i++) {
                for (int j = 0; j < colsA; j++) {
                    result[i, j] = matrixA[i, j] + matrixB[i, j];
                }
            }

            return result;
        }
    }
}
