namespace SmartCondoApi.Infra
{
    public class RegistrationNumberValidator
    {
        //Brasil
        public bool Verify(string registrationNumber)
        {
            registrationNumber = new string([.. registrationNumber.Where(char.IsDigit)]);

            if (registrationNumber.Length == 14)
            {
                return CheckCompanyTaxID(registrationNumber);
            }

            return CheckIndividualTaxID(registrationNumber);
        }

        private static bool CheckIndividualTaxID(string cpf)
        {
            if (cpf.Length != 11 || cpf.All(c => c == cpf[0]))
                return false;

            int[] pesos1 = { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
            int soma1 = cpf.Take(9).Select((c, i) => (c - '0') * pesos1[i]).Sum();
            int digito1 = (soma1 * 10) % 11;
            digito1 = digito1 == 10 ? 0 : digito1;

            int[] pesos2 = { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };
            int soma2 = cpf.Take(10).Select((c, i) => (c - '0') * pesos2[i]).Sum();
            int digito2 = (soma2 * 10) % 11;
            digito2 = digito2 == 10 ? 0 : digito2;

            return cpf.EndsWith($"{digito1}{digito2}");
        }

        private static bool CheckCompanyTaxID(string cnpj)
        {
            if (cnpj.Length != 14 || cnpj.All(c => c == cnpj[0]))
                return false;

            int[] pesos1 = { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
            int soma1 = cnpj.Take(12).Select((c, i) => (c - '0') * pesos1[i]).Sum();
            int digito1 = (11 - (soma1 % 11)) % 10;

            int[] pesos2 = { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
            int soma2 = cnpj.Take(13).Select((c, i) => (c - '0') * pesos2[i]).Sum();
            int digito2 = (11 - (soma2 % 11)) % 10;

            return cnpj.EndsWith($"{digito1}{digito2}");
        }
    }
}
