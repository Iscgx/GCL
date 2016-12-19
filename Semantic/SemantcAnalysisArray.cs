using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Semantic
{
    public partial class SemanticAnalysis
    {
        public void AddArray(string type, string name, bool atDevice, int dimensions, int firstDimensionWidth, params int[] extraDimensionsWidth)
        {
            
            
            if (this.DefinedTypes.ContainsKey(type) == false)
                Error("Undefined Type {0} for variable {1}.", type, name);
            else
            {
                var arrayPrimitive = this.DefinedTypes[type];
                var arrayType = new ArrayType(arrayPrimitive, dimensions, firstDimensionWidth, extraDimensionsWidth);
                var variable = new Variable(name, arrayType, atDevice);
                if (this.blocks.Any())
                {
                    var currentBlock = this.blocks[this.blocks.Count() - 1];
                    if (currentBlock.HasVariable(variable))
                        Error("Previously defined variable \"{0}\"", name);
                    else
                    {
                        currentBlock.AddVariable(variable);
                        var typeName = string.Format("array({0})", type);
                        if (this.definedArrays.ContainsKey(typeName) == false)
                        {
                            this.definedArrays.Add(typeName, arrayType);
                            this.stringByType.Add(arrayType, typeName);
                            this.DefinedTypes.Add(typeName, arrayType);
                        }


                    }
                }
                else
                {
                    if (this.globalSymbolTable.HasVariable(variable))
                        Error("Previously defined variable \"{0}\"", name); //Already exists 
                    else
                        this.globalSymbolTable.AddVariable(variable);
                } 
            }
        }

        public ArrayType GetArrayTypeFrom(string name)
        {
            if (this.blocks.Any())
            {
                for (var i = this.blocks.Count() - 1; i >= 0; i--)
                {
                    if (this.blocks[i].Variables.ContainsKey(name)) //Variable found!!
                    {
                        return (ArrayType) this.blocks[i].Variables[name].Type;
                    }
                }
            }

            if (this.globalSymbolTable.Variables.ContainsKey(name))
                return (ArrayType) this.globalSymbolTable.Variables[name].Type;
            else if (this.globalSymbolTable.DeviceSymbolTable.Variables.ContainsKey(name))
                return (ArrayType) this.globalSymbolTable.DeviceSymbolTable.Variables[name].Type;

            else return null;
        }

        public bool UseArray(string name, bool atDevice, int numberOfDimensions, out string baseType)
        {
            ISymbolTable globalTable;
            if (atDevice)
                globalTable = this.globalSymbolTable.DeviceSymbolTable;
            else
                globalTable = this.globalSymbolTable; 

            if (this.blocks.Any())
            {
                for (var i = this.blocks.Count() - 1; i >= 0; i--)
                {
                    if (this.blocks[i].Variables.ContainsKey(name) && this.blocks[i].Variables[name].Type.Parameters != TypeParameters.IsStructure && (this.blocks[i].Variables[name].Type.Parameters & TypeParameters.IsArray)== TypeParameters.IsArray) //Variable found!!
                    {
                        var arrayType = (this.blocks[i].Variables[name].Type as ArrayType);
                        if (numberOfDimensions != arrayType.Dimensions)
                        {
                            Error("Array {0} expected {1} dimensions but was used with {2}.", name, arrayType.Dimensions, numberOfDimensions);
                            baseType = "error";
                        }
                        else
                            baseType = this.stringByType[arrayType.BaseType];
                        
                        return true;
                    }
                }
            }
            else if (globalTable.Variables.ContainsKey(name) && globalTable.Variables[name].Type.Parameters != TypeParameters.IsStructure && (globalTable.Variables[name].Type.Parameters & TypeParameters.IsArray) == TypeParameters.IsArray) //Variable Found!!!
            {
                var arrayType = (globalTable.Variables[name].Type as ArrayType);
                if (numberOfDimensions != arrayType.Dimensions)
                {
                    Error("Array {0} expected {1} dimensions but was used with {2}.", name, arrayType.Dimensions, numberOfDimensions);
                    baseType = "error";
                }
                else
                    baseType = this.stringByType[arrayType.BaseType];
                return true;
            }
            baseType = "error";
            return false;
        }
    }
}
