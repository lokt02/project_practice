using System;
using Microsoft.Research.SEAL;

namespace homomorphic_encryption
{
    class Program
    {
        static void Main(string[] args)
        {
            EncryptionParameters parms = new EncryptionParameters(SchemeType.BFV);
            parms.PolyModulusDegree = 4096;
            parms.CoeffModulus = CoeffModulus.BFVDefault(4096);
            parms.PlainModulus = new Modulus(1024);


            SEALContext context = new SEALContext(new EncryptionParameters(parms));

            KeyGenerator keygen = new KeyGenerator(context);
            PublicKey publicKey = new PublicKey(keygen.PublicKey);
            SecretKey secretKey = new SecretKey(keygen.SecretKey);

            Encryptor encryptor = new Encryptor(context, publicKey);

            Evaluator evaluator = new Evaluator(context);

            Decryptor decryptor = new Decryptor(context, secretKey);

            int x = 10;
            using Plaintext xPlain = new Plaintext(x.ToString());
            Console.WriteLine($"Express x = {x} as a plaintext polynomial 0x{xPlain}.");

            Ciphertext xEncrypted = new Ciphertext();
            encryptor.Encrypt(xPlain, xEncrypted);
            Plaintext xDecrypted = new Plaintext();

            decryptor.Decrypt(xEncrypted, xDecrypted);
            Console.WriteLine($"{xDecrypted}");

            Console.WriteLine("");
            Console.WriteLine("x^2 + 1");
            using Ciphertext xSqPlusOne = new Ciphertext();
            evaluator.Square(xEncrypted, xSqPlusOne);

            using Plaintext plainOne = new Plaintext("1");
            evaluator.AddPlainInplace(xSqPlusOne, plainOne);

            Plaintext decryptedResult = new Plaintext();
            decryptor.Decrypt(xSqPlusOne, decryptedResult);
            Console.WriteLine($"{decryptedResult}");

            ////////////////////////////

            int y = 6;
            Plaintext yPlain = new Plaintext(y.ToString());

            Ciphertext yEncrypted = new Ciphertext();
            encryptor.Encrypt(yPlain, yEncrypted);
            Plaintext yDecrypted = new Plaintext();

            Ciphertext addValueEncrypted = new Ciphertext();
            evaluator.Add(yEncrypted, xEncrypted, addValueEncrypted);
            Plaintext addValue = new Plaintext();
            decryptor.Decrypt(addValueEncrypted, addValue);
            Console.WriteLine("x + 6");
            Console.WriteLine(addValue);

            Ciphertext multipleValueEncrypted = new Ciphertext();
            evaluator.Multiply(yEncrypted, xEncrypted, multipleValueEncrypted);
            Plaintext multipleValue = new Plaintext();
            decryptor.Decrypt(multipleValueEncrypted, multipleValue);
            Console.WriteLine("x * 6");
            Console.WriteLine(multipleValue);

            Plaintext plainFour = new Plaintext("4");
            Plaintext plainValue = new Plaintext();
            evaluator.MultiplyPlainInplace(multipleValueEncrypted, plainFour);
            decryptor.Decrypt(multipleValueEncrypted, plainValue);
            Console.WriteLine("x * 6 * 4 (but 4 is plaintext)");
            Console.WriteLine(plainValue);
            //Plaintext plainFour = new Plaintext("4");
            //Plaintext xValue = new Plaintext();
            //evaluator.MultiplyPlainInplace(xEncrypted, plainFour);
            //decryptor.Decrypt(xEncrypted, xValue);
            //Console.WriteLine("x * 4 (but 4 is plaintext)");
            //Console.WriteLine(xValue);
        }
    }
}
