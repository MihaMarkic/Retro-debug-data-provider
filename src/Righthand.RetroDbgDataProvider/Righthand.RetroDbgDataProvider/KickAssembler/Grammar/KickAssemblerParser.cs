using static Righthand.RetroDbgDataProvider.KickAssembler.KickAssemblerLexer;

namespace Righthand.RetroDbgDataProvider.KickAssembler;

partial class KickAssemblerParser
{
    internal bool IsEolPrevious()
    {
        int idx = CurrentToken.TokenIndex;
        int ch;
        
        do
        {
            ch = TokenStream.Get(--idx).Channel;
        }
        while (ch is not (EOL_CHANNEL or 0));
        return ch == EOL_CHANNEL;
    }
}