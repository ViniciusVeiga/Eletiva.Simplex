using Eletiva.Simplex.Entities;
using System;
using System.Collections.Generic;
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
        private static List<BaseVariableValue> _baseVariableValue;
        private static List<VariableValue> _variableValues;
        private static List<BaseValue> _baseValues;
        private static TotalValue _totalValue;

        static void Main(string[] args)
        {
            FetchTable();
            PrintTable();
            Iteration();
            PrintResult();
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
            var base_01 = new Base("x3");
            var base_02 = new Base("x4");
            var base_03 = new Base("x5");
            var variable_01 = new Variable("x1");
            var variable_02 = new Variable("x2");
            var variable_03 = new Variable("x3");
            var variable_04 = new Variable("x4");
            var variable_05 = new Variable("x5");
            _baseVariableValue = new List<BaseVariableValue>
            {
                new BaseVariableValue(base_01, variable_01, 1), new BaseVariableValue(base_01, variable_02, 0), new BaseVariableValue(base_01, variable_03, 1), new BaseVariableValue(base_01, variable_04, 0), new BaseVariableValue(base_01, variable_05, 0),
                new BaseVariableValue(base_02, variable_01, 0), new BaseVariableValue(base_02, variable_02, 2), new BaseVariableValue(base_02, variable_03, 0), new BaseVariableValue(base_02, variable_04, 1), new BaseVariableValue(base_02, variable_05, 0),
                new BaseVariableValue(base_03, variable_01, 3), new BaseVariableValue(base_03, variable_02, 2), new BaseVariableValue(base_03, variable_03, 0), new BaseVariableValue(base_03, variable_04, 0), new BaseVariableValue(base_03, variable_05, 1)
            };
            _baseValues = new List<BaseValue>
            {
                new BaseValue(base_01, 4), new BaseValue(base_02, 12), new BaseValue(base_03, 18)
            };
            _variableValues = new List<VariableValue>
            {
                new VariableValue(variable_01, -3), new VariableValue(variable_02, -5), new VariableValue(variable_03, 0), new VariableValue(variable_04, 0), new VariableValue(variable_05, 0)
            };
            _totalValue = new TotalValue(0);
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
