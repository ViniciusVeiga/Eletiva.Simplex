using Eletiva.Simplex.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Eletiva.Simplex
{
    public class Program
    {
        /// <summary>
        /// Classes usadas para montar a tabela
        /// BaseVariableValue: Valores centrais, convergem em Base X Variavel
        /// VariableValue: Valores da linha Z, que estão na ultima linha da tabela
        /// BaseValue: Valores da coluna b, que estão na ultima coluna da tabela
        /// TotalValor: Função Objetivo
        /// </summary>
        private static List<BaseVariableValue> _baseVariableValue = new List<BaseVariableValue>();
        private static List<VariableValue> _variableValues = new List<VariableValue>();
        private static List<BaseValue> _baseValues = new List<BaseValue>();
        private static TotalValue _totalValue;

        static void Main(string[] args)
        {
            // Busca e monta tabela
            FetchTable();
            // Mostrar tabela
            PrintTable();
            // Começar iteração
            Iteration();
            // Mostrar resultado
            PrintResult();
            // Parar para ler
            Console.Read();
        }

        #region Print Result

        /// <summary>
        /// Método que mostra o resultado
        /// </summary>
        private static void PrintResult()
        {
            Console.WriteLine("\n");
            var variables = _variableValues.Select(v => v.Variable.Name);
            var basics = _baseValues.Select(v => v.Base.Name);
            var notBasic = string.Join(" = ", variables.Except(basics));
            Console.WriteLine($"Variáveis não-básicas (VNB): {notBasic} = 0");
            Console.WriteLine("Variáveis básicas (VB): ");
            foreach (var basic in _baseValues)
            {
                Console.WriteLine($"{basic.Base.Name} = {basic.Value}");
            }
            Console.WriteLine($"E a Função Objetivo: Z = {_totalValue.Value}");
        }

        #endregion

        #region Print Table

        /// <summary>
        /// Método que mostra a tabela na tela
        /// </summary>
        private static void PrintTable()
        {
            Console.WriteLine();
            Console.WriteLine();
            Console.Write($"Base\t");
            foreach (var variable in _variableValues)
            {
                Console.Write($"{variable.Variable.Name}\t");
            }
            Console.Write($"b\t");
            Console.WriteLine();
            foreach (var baseValue in _baseValues)
            {
                Console.Write($"{baseValue.Base.Name}\t");
                foreach (var baseVariableValue in _baseVariableValue.Where(t => baseValue.Base.Id.Equals(t.Base.Id)))
                {
                    Console.Write($"{baseVariableValue.Value}\t");
                }
                Console.Write($"{_baseValues.Where(t => baseValue.Base.Id.Equals(t.Base.Id)).First().Value}");
                Console.WriteLine();
            }
            Console.Write("Z\t");
            foreach (var variableValue in _variableValues)
            {
                Console.Write($"{variableValue.Value}\t");
            }
            Console.Write($"{_totalValue.Value}\t");
        }

        #endregion

        #region Create Table

        /// <summary>
        /// Método que lê o arquivo e cria os objetos que representam a tabela
        /// </summary>
        private static void FetchTable()
        {
            var textFile = File.ReadAllText("exemplo 01.txt");
            var splited = textFile.Split("\r\n");
            var splitedVariable = splited[0].Split(' ');
            // Preenchendo os valores da ultima linha
            FetchVariableValues(splitedVariable);
            // Preenchendo os valores da ultima coluna
            FetchBaseValues(splited);
            // Preenchendo os valores Base X Variavel
            FetchBaseVariableValues(splited);
            // Completando o resto
            FillRest();
            // Criando a função objetiva
            _totalValue = new TotalValue(0);
        }

        private static void FillRest()
        {
            foreach (var baseValue in _baseValues)
            {
                var variableValue = new VariableValue(new Variable(baseValue.Base.Name, baseValue.Base.Index), 0);
                _variableValues.Add(variableValue);
                foreach (var baseValue2 in _baseValues)
                {
                    // Quando a variavel tiver o mesmo nome que a base, valor igual a 1
                    if (variableValue.Variable.Name.Equals(baseValue2.Base.Name))
                        _baseVariableValue.Add(new BaseVariableValue(baseValue2.Base, variableValue.Variable, 1));
                    else
                        _baseVariableValue.Add(new BaseVariableValue(baseValue2.Base, variableValue.Variable, 0));
                }
            }
        }

        private static void FetchBaseVariableValues(string[] splited)
        {
            var skipedFirstLine = splited.Skip(1);
            for (int i = 0; i < _baseValues.Count(); i++)
            {
                var baseValue = _baseValues[i];
                var baseLine = skipedFirstLine.ElementAt(i);
                for (int j = 0; j < _variableValues.Count(); j++)
                {
                    var variableValue = _variableValues[j];
                    var value = int.Parse(baseLine.Split(' ').ElementAt(j));
                    _baseVariableValue.Add(new BaseVariableValue(baseValue.Base, variableValue.Variable, value));
                }
            }
        }

        private static void FetchBaseValues(string[] splited)
        {
            var maxVariableCount = _variableValues.Max(v => v.Variable.Index) + 1;
            for (int i = 0; i < splited.Count() - 1; i++)
            {
                var baseSplited = splited.Except(splited.Take(1)).ElementAt(i).Split(' ');
                var value = int.Parse(baseSplited.Last());
                _baseValues.Add(new BaseValue(new Base($"x{i + maxVariableCount}", i + maxVariableCount), value));
            }
        }

        private static void FetchVariableValues(string[] splitedVariable)
        {
            _variableValues = splitedVariable.Select((variable, index) =>
            {
                var value = decimal.Parse(splitedVariable[index]);
                return new VariableValue(new Variable($"x{index + 1}", index + 1), value * -1);
            })
            .ToList();
        }

        #endregion

        #region Iteration

        /// <summary>
        /// Inicia a iteração e verifica se a linha Z tem algum valor menor que 0
        /// Se sim, sai da função pois foi finalizada
        /// </summary>
        private static void Iteration()
        {
            while (_variableValues.Any(v => v.Value < 0M))
            {
                // Pega a variavel que vai tombar para a base
                var variableToEnter = GetVariableThatWillEnter();
                // Pega a base que vai sair
                var baseToChange = GetBaseThatComeOut(variableToEnter);
                // Troca a base com a variavel
                baseToChange.ChangeBase(variableToEnter);
                // Começa o ajuste dos valores da tabela
                Calculate(baseToChange, variableToEnter);
                // Mostra a tabela na tela
                PrintTable();
            }
        }

        private static void Calculate(Base @base, Variable variable)
        {
            // Primeiro ajusta a linha central, deixando o Valor central como 1 e ajustando as outras colunas
            var centerValue = GetBaseVariableValue(@base, variable);
            var value = 1 / centerValue.Value;
            var baseVariableValues = _baseVariableValue.Where(bv => @base.Id.Equals(bv.Base.Id)).ToList();
            baseVariableValues.ForEach(baseVariableValue => baseVariableValue.ChangeValue(baseVariableValue.Value * value));
            var baseValue = _baseValues.First(b => @base.Id.Equals(b.Base.Id));
            baseValue.ChangeValue(baseValue.Value * value);

            // Começa o ajuste das outras colunas, para zera-las, passando a base, variavel, e a linha da base
            CalculateOtherLines(@base, variable, baseVariableValues);
        }

        private static void CalculateOtherLines(Base @base, Variable variable, List<BaseVariableValue> baseLine)
        {
            var centerValue = GetBaseVariableValue(@base, variable);
            var otherBases = _baseVariableValue.Except(baseLine).ToList();
            var centers = otherBases.Where(bv => variable.Id.Equals(bv.Variable.Id)).Select(bv => bv).ToList();
            centers.ForEach(center =>
            {
                // Calcula os valores são Base X Variavel e a coluna "b" (ultima coluna)
                CaculateBaseVariableValuesAndBaseValues(baseLine, center, centerValue, @base);
                // Calcula a linha de Z e o valor objetivo de Z
                CalculateVariableValuesAndTotalValue(variable, baseLine, centerValue);
            });
        }

        private static void CaculateBaseVariableValuesAndBaseValues(List<BaseVariableValue> baseLine, BaseVariableValue center, BaseVariableValue centerValue, Base @base)
        {
            var value = center.Value / centerValue.Value;
            baseLine.ForEach(bv =>
            {
                var valueToSubtract = bv.Value * value;
                var baseVariableValue = GetBaseVariableValue(center.Base, bv.Variable);
                baseVariableValue.ChangeValue(baseVariableValue.Value - valueToSubtract);
            });
            var baseValue = _baseValues.Where(b => @base.Id.Equals(b.Base.Id)).First();
            var valueToSubtract = baseValue.Value * value;
            var baseValueToChange = _baseValues.Where(b => center.Base.Id.Equals(b.Base.Id)).First();
            baseValueToChange.ChangeValue(baseValueToChange.Value - valueToSubtract);
        }

        private static void CalculateVariableValuesAndTotalValue(Variable variable, List<BaseVariableValue> baseLine, BaseVariableValue centerValue)
        {
            var variableValue = _variableValues.Where(v => variable.Id.Equals(v.Variable.Id)).First();
            var value = variableValue.Value / centerValue.Value;
            baseLine.ForEach(bv =>
            {
                var valueToSubtract = bv.Value * value;
                var variableValue = _variableValues.Where(v => bv.Variable.Id.Equals(v.Variable.Id)).First();
                variableValue.ChangeValue(variableValue.Value - valueToSubtract);
            });
            var baseValue = _baseValues.Where(b => centerValue.Base.Id.Equals(b.Base.Id)).First();
            var valueToSubtract = baseValue.Value * value;
            _totalValue.ChangeValue(_totalValue.Value - valueToSubtract);
        }

        private static BaseVariableValue GetBaseVariableValue(Base @base, Variable variable) =>
            _baseVariableValue.First(value => @base.Id.Equals(value.Base.Id) && variable.Id.Equals(value.Variable.Id));

        private static Base GetBaseThatComeOut(Variable variable)
        {
            var lowerRatio = _baseValues.Select(baseValue =>
            {
                var baseVariableValue = GetBaseVariableValue(baseValue.Base, variable);
                return (0M.Equals(baseVariableValue.Value) ? 0M : baseValue.Value / baseVariableValue.Value, baseValue.Base);
            })
            .Where(l => l.Item1 != 0M)
            .ToList();
            var minValue = lowerRatio.Min(l => l.Item1);
            return lowerRatio.First(l => minValue.Equals(l.Item1)).Base;
        }

        private static Variable GetVariableThatWillEnter()
        {
            var minVariableValue = _variableValues.Min(variableValue => variableValue.Value);
            var variable = _variableValues.Where(variableValue => minVariableValue.Equals(variableValue.Value)).First().Variable;
            return variable;
        }

        #endregion
    }
}
