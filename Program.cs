using System;
using System.IO;
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
            parms.PlainModulus = new Modulus(4096 * 2);


            SEALContext context = new SEALContext(new EncryptionParameters(parms));

            Console.WriteLine(context.ParameterErrorMessage());
            KeyGenerator keygen = new KeyGenerator(context);
            PublicKey publicKey = new PublicKey(keygen.PublicKey);
            SecretKey secretKey = new SecretKey(keygen.SecretKey);

            Encryptor encryptor = new Encryptor(context, publicKey);

            Evaluator evaluator = new Evaluator(context);

            Decryptor decryptor = new Decryptor(context, secretKey);


            Console.WriteLine("Generate locally usable relinearization keys.");
            using RelinKeys relinKeys = keygen.RelinKeysLocal();

            int x = 6;
            using Plaintext xPlain = new Plaintext(x.ToString());

            using Ciphertext xEncrypted = new Ciphertext();
            encryptor.Encrypt(xPlain, xEncrypted);
            using Ciphertext xSqPlusOne = new Ciphertext();
            using Plaintext decryptedResult = new Plaintext();
            using Ciphertext xPlusOneSq = new Ciphertext();
            using Ciphertext encryptedResult = new Ciphertext();
            using Plaintext plainOne = new Plaintext("1");
            using Plaintext plainFour = new Plaintext("4");
            /*
            We now repeat the computation relinearizing after each multiplication.
            */

            Console.WriteLine("Compute and relinearize xSquared (x^2),");
            Console.WriteLine(new string(' ', 13) + "then compute xSqPlusOne (x^2+1)");
            using Ciphertext xSquared = new Ciphertext();
            evaluator.Square(xEncrypted, xSquared);
            Console.WriteLine($"    + size of xSquared: {xSquared.Size}");
            evaluator.RelinearizeInplace(xSquared, relinKeys);
            Console.WriteLine("    + size of xSquared (after relinearization): {0}",
                xSquared.Size);
            evaluator.AddPlain(xSquared, plainOne, xSqPlusOne);
            Console.WriteLine("    + noise budget in xSqPlusOne: {0} bits",
                decryptor.InvariantNoiseBudget(xSqPlusOne));
            Console.Write("    + decryption of xSqPlusOne: ");
            decryptor.Decrypt(xSqPlusOne, decryptedResult);
            int q = Convert.ToInt32(decryptedResult.ToString(), 16);
            Console.WriteLine($"{q} ...... Correct.");
            //Console.WriteLine($"0x{decryptedResult} ...... Correct.");


            using Ciphertext xPlusOne = new Ciphertext();
            Console.WriteLine("Compute xPlusOne (x+1),");
            Console.WriteLine(new string(' ', 13) +
                "then compute and relinearize xPlusOneSq ((x+1)^2).");
            evaluator.AddPlain(xEncrypted, plainOne, xPlusOne);
            evaluator.Square(xPlusOne, xPlusOneSq);
            Console.WriteLine($"    + size of xPlusOneSq: {xPlusOneSq.Size}");
            evaluator.RelinearizeInplace(xPlusOneSq, relinKeys);
            Console.WriteLine("    + noise budget in xPlusOneSq: {0} bits",
                decryptor.InvariantNoiseBudget(xPlusOneSq));
            Console.Write("    + decryption of xPlusOneSq: ");
            decryptor.Decrypt(xPlusOneSq, decryptedResult);
            int r = Convert.ToInt32(decryptedResult.ToString(), 16);
            Console.WriteLine($"{r} ...... Correct.");


            Console.WriteLine("Compute and relinearize encryptedResult (4(x^2+1)(x+1)^2).");
            evaluator.MultiplyPlainInplace(xSqPlusOne, plainFour);
            evaluator.Multiply(xSqPlusOne, xPlusOneSq, encryptedResult);
            Console.WriteLine($"    + size of encryptedResult: {encryptedResult.Size}");
            evaluator.RelinearizeInplace(encryptedResult, relinKeys);
            Console.WriteLine("    + size of encryptedResult (after relinearization): {0}",
                encryptedResult.Size);
            Console.WriteLine("    + noise budget in encryptedResult: {0} bits",
                decryptor.InvariantNoiseBudget(encryptedResult));

            Console.WriteLine();
            Console.WriteLine("NOTE: Notice the increase in remaining noise budget.");

            /*
            Relinearization clearly improved our noise consumption. We have still plenty
            of noise budget left, so we can expect the correct answer when decrypting.
            */

            Console.WriteLine("Decrypt encrypted_result (4(x^2+1)(x+1)^2).");
            decryptor.Decrypt(encryptedResult, decryptedResult);
            int t = Convert.ToInt32(decryptedResult.ToString(), 16);
            Console.WriteLine("    + decryption of 4(x^2+1)(x+1)^2 = 0x{0} ...... Correct.", t);


            Plaintext four = new Plaintext("4");
            Plaintext four1 = new Plaintext("5");
            Ciphertext fourEncrypted = new Ciphertext();
            Ciphertext four1Encrypted = new Ciphertext();
            encryptor.Encrypt(four, fourEncrypted);
            encryptor.Encrypt(four1, four1Encrypted);

            Ciphertext d = new Ciphertext();
            evaluator.Sub(fourEncrypted, four1Encrypted, d);
            Plaintext temp = new Plaintext();
            decryptor.Decrypt(d, temp);

            Console.WriteLine($"{Convert.ToInt32(temp.ToString(), 16)}");
        }
    }
}
