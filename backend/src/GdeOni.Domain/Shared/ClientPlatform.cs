namespace GdeOni.Domain.Shared;

/// <summary>
/// D16. Тип клиента, инициировавшего оплату подписки. Нужен, чтобы
/// провайдер (YooKassa) вернул юзера на правильный URL:
/// на мобилке — deep-link <c>gdeoni://payment/return</c>, на вебе —
/// страница <c>/payment/return</c> с автопоол-обновлением статуса.
///
/// Значение <see cref="Mobile"/> = 0 умышленно: старые mobile-клиенты
/// (deployed apk) не шлют это поле в теле create-payment, десериализатор
/// подставит default (0). Это позволяет обновить бэк без принуждения
/// апдейта мобильных клиентов.
/// </summary>
public enum ClientPlatform
{
    Mobile = 0,
    Web = 1,
}
