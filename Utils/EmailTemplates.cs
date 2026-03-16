namespace FilmesAPI.Utils;

public static class EmailTemplates
{
    public static string GetConfirmacaoTemplate(string nome, string linkConfirmacao)
    {
        return $@"
<div style='background-color: #121214; font-family: Arial, sans-serif; max-width: 600px; margin: auto; padding: 40px 20px; border-radius: 15px; color: #ffffff; text-align: center;'>
    
    <h1 style='color: #ffffff; font-size: 32px; margin-bottom: 20px; font-weight: bold;'>
        M<span style='color: #fbc02d;'>oo</span>v
    </h1>

    <div style='background-color: #1e1e20; padding: 30px; border-radius: 10px; border: 1px solid #333;'>
        <h2 style='color: #ffffff; margin-top: 0;'>Bem-vindo ao time!</h2>
        
        <p style='color: #b3b3b3; font-size: 16px; line-height: 1.5;'>
            Olá, {nome}! Sua conta no <strong>Moov Cine</strong> está quase pronta. 
            Falta apenas um passo para você explorar o melhor do cinema.
        </p>

        <p style='color: #b3b3b3; font-size: 16px; margin-bottom: 30px;'>
            Clique no botão abaixo para ativar sua conta:
        </p>

        <div style='margin: 40px 0;'>
            <a href='{linkConfirmacao}' style='background-color: #fbc02d; color: #000000; padding: 18px 35px; text-decoration: none; border-radius: 8px; font-weight: bold; font-size: 16px; box-shadow: 0 4px 15px rgba(251, 192, 45, 0.3);'>
                ATIVAR MINHA CONTA
            </a>
        </div>

        <p style='font-size: 13px; color: #777; margin-top: 30px;'>
            Se você não solicitou este cadastro, pode ignorar este e-mail com segurança.
        </p>
    </div>

    <p style='font-size: 11px; color: #555; margin-top: 20px;'>
        © 2026 Moov Cine - O cinema na palma da sua mão.
    </p>
</div>";
    }

    public static string GetRedefinicaoTemplate(string nome, string linkConfirmacao)
    {
        return $@"
<div style='background-color: #121214; font-family: Arial, sans-serif; max-width: 600px; margin: auto; padding: 40px 20px; border-radius: 15px; color: #ffffff; text-align: center;'>
    
    <h1 style='color: #ffffff; font-size: 32px; margin-bottom: 20px; font-weight: bold;'>
        M<span style='color: #fbc02d;'>oo</span>v
    </h1>

    <div style='background-color: #1e1e20; padding: 30px; border-radius: 10px; border: 1px solid #333;'>
        <h2 style='color: #ffffff; margin-top: 0;'>Bem-vindo ao time!</h2>
        
        <p style='color: #b3b3b3; font-size: 16px; line-height: 1.5;'>
            Olá, {nome}!  
        </p>

        <p style='color: #b3b3b3; font-size: 16px; margin-bottom: 30px;'>
            Clique no botão abaixo para redefinir sua senha no <strong>Moov Cine</strong>.
        </p>

        <div style='margin: 40px 0;'>
            <a href='{linkConfirmacao}' style='background-color: #fbc02d; color: #000000; padding: 18px 35px; text-decoration: none; border-radius: 8px; font-weight: bold; font-size: 16px; box-shadow: 0 4px 15px rgba(251, 192, 45, 0.3);'>
                REDEFINIR MINHA SENHA
            </a>
        </div>

        <p style='font-size: 13px; color: #777; margin-top: 30px;'>
            Se esta solicitação não foi sua, ignore este e-mail.
        </p>
    </div>

    <p style='font-size: 11px; color: #555; margin-top: 20px;'>
        © 2026 Moov Cine - O cinema na palma da sua mão.
    </p>
</div>";
    }
}