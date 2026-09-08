namespace TheiaLang;

public static class TMath
{
    public static long RoundUp(long value, long alignment)
    {
        if(value % alignment == 0)
            return value;
        
        return alignment * (1 + value / alignment);
    }
}