namespace FindMe.Models;

// Os nomes ficam em MAIÚSCULAS de propósito: são os mesmos valores do contrato da API Java
// (JSON e coluna do banco), assim Mobile/DevOps não percebem diferença entre as duas versões.
public enum TipoUsuario
{
    RESPONSAVEL,
    COLABORADOR,
    MODERADOR
}
