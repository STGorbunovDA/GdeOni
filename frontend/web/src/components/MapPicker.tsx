import { useEffect, useRef } from 'react';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';

type MapPickerProps = {
  /** Текущая точка (из полей ввода / геолокации). null — точки ещё нет. */
  latitude: number | null;
  longitude: number | null;
  /** Клик по карте — отдаёт координаты точки в форму. */
  onPick: (latitude: number, longitude: number) => void;
  /** Высота карты в px. */
  height?: number;
};

const DEFAULT_CENTER: [number, number] = [55.751244, 37.618423]; // Москва
const DEFAULT_ZOOM = 10;
const PICK_ZOOM = 17;

/**
 * Leaflet default-иконка тянет png-ассеты, которые ломаются в бандлере
 * (Vite не резолвит относительные пути marker-icon.png). Рисуем пин через
 * divIcon эмодзи — без картиночных файлов, кросс-платформенно.
 */
const pinIcon = L.divIcon({
  className: 'gdeoni-map-pin',
  html: '<div style="font-size:30px;line-height:1">📍</div>',
  iconSize: [30, 30],
  // Кончик булавки эмодзи — снизу по центру символа.
  iconAnchor: [9, 30],
});

/**
 * F5+. Карта-пикер координат. Тайлы — OpenStreetMap (без API-ключей).
 * Клик по карте отдаёт координаты; внешний lat/lon (ручной ввод или
 * «Получить координаты») синхронизирует маркер. Первое появление точки
 * зумит карту на неё, дальше клики только двигают маркер, не дёргая карту.
 */
export function MapPicker({
  latitude,
  longitude,
  onPick,
  height = 280,
}: MapPickerProps) {
  const containerRef = useRef<HTMLDivElement | null>(null);
  const mapRef = useRef<L.Map | null>(null);
  const markerRef = useRef<L.Marker | null>(null);
  const focusedRef = useRef(latitude != null && longitude != null);

  // onPick в ref — чтобы init-эффект не пересоздавал карту при смене колбэка.
  const onPickRef = useRef(onPick);
  onPickRef.current = onPick;

  // Стартовые центр/зум фиксируем на маунте (ref стабилен → не нужен в deps).
  const initialView = useRef<{ center: [number, number]; zoom: number }>({
    center:
      latitude != null && longitude != null
        ? [latitude, longitude]
        : DEFAULT_CENTER,
    zoom: latitude != null && longitude != null ? PICK_ZOOM : DEFAULT_ZOOM,
  });

  useEffect(() => {
    if (!containerRef.current || mapRef.current) return;
    const map = L.map(containerRef.current).setView(
      initialView.current.center,
      initialView.current.zoom,
    );
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '&copy; OpenStreetMap',
      maxZoom: 19,
    }).addTo(map);
    // Убираем дефолтный префикс Leaflet (в 1.9 он содержит флаг Украины) —
    // оставляем только «© OpenStreetMap» (без флага/страны; требуется
    // лицензией OSM на тайлы).
    map.attributionControl.setPrefix(false);
    map.on('click', (e: L.LeafletMouseEvent) => {
      // Клик — пользователь сам выбрал и точку, и текущий вид карты.
      // Помечаем focused, чтобы sync-эффект НЕ центрировал и не зумил
      // карту (иначе первый клик «прыгал» и увеличивал масштаб). Зум/
      // панораму дальше меняет только сам пользователь.
      focusedRef.current = true;
      onPickRef.current(e.latlng.lat, e.latlng.lng);
    });
    mapRef.current = map;
    // Контейнер мог отрисоваться с нулевой шириной (внутри Card/Stack) —
    // форсим пересчёт размеров после layout, иначе тайлы серые.
    setTimeout(() => map.invalidateSize(), 0);

    return () => {
      map.remove();
      mapRef.current = null;
      markerRef.current = null;
    };
  }, []);

  // Синхронизация маркера при внешнем изменении координат.
  useEffect(() => {
    const map = mapRef.current;
    if (!map) return;
    if (latitude == null || longitude == null) {
      markerRef.current?.remove();
      markerRef.current = null;
      return;
    }
    const pos: [number, number] = [latitude, longitude];
    if (markerRef.current) {
      markerRef.current.setLatLng(pos);
    } else {
      markerRef.current = L.marker(pos, { icon: pinIcon }).addTo(map);
    }
    // Первое появление точки — центрируем и зумим. Дальше карту не дёргаем
    // (клики по краю не должны рецентрить вид).
    if (!focusedRef.current) {
      map.setView(pos, PICK_ZOOM);
      focusedRef.current = true;
    }
  }, [latitude, longitude]);

  return (
    <div
      ref={containerRef}
      style={{
        height,
        width: '100%',
        borderRadius: 12,
        overflow: 'hidden',
      }}
    />
  );
}
