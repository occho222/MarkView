namespace MarkView.Services
{
    public interface IPlantUmlService
    {
        string ProcessPlantUmlCode(string plantUmlCode);
        string GeneratePlantUmlImageUrl(string plantUmlCode);
        bool IsPlantUmlCodeBlock(string language);
        string EncodeForPlantUml(string content);
    }
}