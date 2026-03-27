// =============================================================================
// MAS.Communication
// https://www.mas-automation.com/
//
// Copyright 2026 MAS (厦门威光) Corporation
//
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.
// =============================================================================

using MAS.Communication;

namespace MAS.CommunicationUnitTest.McProtocol;

internal class TestDataHelper {
    public static T GenerateRandomTestData<T>() where T : struct {
        Random rand = new();
        var structType = typeof(T);
        var result = new T();

        foreach (var field in structType.GetFields()) {
            if (field.FieldType == typeof(short)) {
                field.SetValueDirect(__makeref(result), (short)rand.Next(short.MinValue, short.MaxValue));
            } else if (field.FieldType == typeof(float)) {
                float randomFloat = (float)(rand.NextDouble() * 3.14159);
                field.SetValueDirect(__makeref(result), randomFloat);
            } else if (field.FieldType == typeof(int)) {
                field.SetValueDirect(__makeref(result), rand.Next(int.MinValue, int.MaxValue));
            } else if (field.FieldType == typeof(double)) {
                double randomDouble = rand.NextDouble() * 3.141592653589;
                field.SetValueDirect(__makeref(result), randomDouble);
            } else if (field.FieldType == typeof(string)) {
                int length = 10;
                if (field.GetCustomAttributes(typeof(FixedStringAttribute), false).FirstOrDefault() is FixedStringAttribute attribute) {
                    length = attribute.Length;
                }

                var randomString = GenerateRandomString(length, rand);
                field.SetValueDirect(__makeref(result), randomString);
            } else if (field.FieldType == typeof(bool)) {
                bool randomBool = rand.Next(0, 2) == 0;
                field.SetValueDirect(__makeref(result), randomBool);
            }
        }

        return result;
    }

    public static string GetStructValues(object structValue, int startAddress) {
        var structType = structValue.GetType();
        var fields = structType.GetFields();

        var boolFields = fields.Where(f => f.FieldType == typeof(bool)).ToList();
        var otherFields = fields.Where(f => f.FieldType != typeof(bool)).ToList();

        int currentAddressOffset = 0;
        int bitOffset = 0;

        var fieldValues = new List<string>();

        foreach (var field in boolFields) {
            string addressInfo = $"地址：D{startAddress + currentAddressOffset}.{bitOffset:X}";
            double currentBytes = 0.125;
            fieldValues.Add($"{field.Name}: {field.GetValue(structValue)},  {addressInfo}, {currentBytes} 字节");

            bitOffset++;
            if (bitOffset >= 16) {
                bitOffset = 0;
                currentAddressOffset++;
            }
        }

        if (bitOffset > 0) {
            bitOffset = 0;
            currentAddressOffset++;
        }

        foreach (var field in otherFields) {
            string addressInfo = "";
            double currentBytes = 0;

            if (field.FieldType == typeof(short)) {
                addressInfo = $"地址：D{startAddress + currentAddressOffset}";
                currentAddressOffset += 1;
                currentBytes += 2;
            } else if (field.FieldType == typeof(int)) {
                addressInfo = $"地址：D{startAddress + currentAddressOffset}";
                currentAddressOffset += 2;
                currentBytes += 4;
            } else if (field.FieldType == typeof(float)) {
                addressInfo = $"地址：D{startAddress + currentAddressOffset}";
                currentAddressOffset += 2;
                currentBytes += 4;
            } else if (field.FieldType == typeof(double)) {
                addressInfo = $"地址：D{startAddress + currentAddressOffset}";
                currentAddressOffset += 4;
                currentBytes += 8;
            } else if (field.FieldType == typeof(string)) {
                int addressCount = 0;

                if (field.GetCustomAttributes(typeof(FixedStringAttribute), false)
                                     .FirstOrDefault() is FixedStringAttribute attribute) {
                    int length = attribute.Length;
                    addressCount = (int)Math.Ceiling(length / 2.0);
                }

                addressInfo = $"地址：D{startAddress + currentAddressOffset}";
                currentAddressOffset += addressCount;
                currentBytes += addressCount * 2;
            } else {
                addressInfo = "地址：N/A (不支持的类型)";
            }

            fieldValues.Add($"{field.Name}: {field.GetValue(structValue)},  {addressInfo}, {currentBytes} 字节");
        }

        return string.Join("\n", fieldValues);
    }

    private static string GenerateRandomString(int length, Random rand) {
        const int asciiMin = 32;
        const int asciiMax = 126;

        var stringChars = new char[length];
        for (int i = 0; i < length; i++) {
            stringChars[i] = (char)rand.Next(asciiMin, asciiMax + 1);
        }

        return new string(stringChars);
    }
}
