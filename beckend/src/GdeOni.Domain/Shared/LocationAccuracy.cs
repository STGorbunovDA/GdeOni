namespace GdeOni.Domain.Shared;

public enum LocationAccuracy
{
    /// <summary>
    /// Точные координаты могилы
    /// </summary>
    Exact = 1,
    /// <summary>
    /// Координаты кладбища
    /// </summary>
    Cemetery = 2,
    /// <summary>
    /// Только город
    /// </summary>
    City = 3,
    /// <summary>
    /// Приблизительное место
    /// </summary>
    Approximate = 4 
}