import { useEffect, useRef } from 'react';
import { useComputedColorScheme } from '@mantine/core';
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
 * Тайлы CARTO basemaps (без API-ключа): Positron для светлой темы,
 * Dark Matter для тёмной — приглушённые карты, спокойнее «сырого» OSM и
 * совпадают с темой сайта. Данные всё те же OpenStreetMap, поэтому в
 * атрибуции указаны и OSM, и CARTO (обязательно по условиям обоих).
 */
const CARTO_LIGHT = 'https://{s}.basemaps.cartocdn.com/light_all/{z}/{x}/{y}.png';
const CARTO_DARK = 'https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}.png';
const CARTO_ATTRIBUTION = '&copy; OpenStreetMap &copy; CARTO';
const tileUrlForScheme = (scheme: 'light' | 'dark') =>
  scheme === 'dark' ? CARTO_DARK : CARTO_LIGHT;

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
 * F5+. Карта-пикер координат. Тайлы — CARTO basemaps (без API-ключей),
 * светлые/тёмные под тему сайта. Клик по карте отдаёт координаты и двигает
 * маркер, НЕ меняя вид (иначе клик у края «прыгал» бы). Внешнее изменение
 * lat/lon («Получить координаты» или ручной ввод) синхронизирует маркер И
 * центрирует карту на точке, чтобы маркер был виден.
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
  const tileLayerRef = useRef<L.TileLayer | null>(null);
  // Какая схема сейчас применена к тайлам — чтобы не дёргать setUrl впустую.
  const appliedSchemeRef = useRef<'light' | 'dark' | null>(null);

  // Тема сайта → светлые/тёмные тайлы. Читаем реальное значение сразу
  // (getInitialValueInEffect:false): атрибут схемы стоит на <html> ещё до
  // первого рендера (см. index.html), поэтому тёмный юзер сразу получает
  // тёмную карту без лишней перезагрузки тайлов.
  const computed = useComputedColorScheme('light', {
    getInitialValueInEffect: false,
  });
  const computedRef = useRef(computed);
  computedRef.current = computed;
  // Клик по карте выставляет этот флаг, чтобы sync-эффект НЕ рецентрил вид
  // на клик. Геолокация/ручной ввод флаг не ставят → карта центрируется на
  // новой точке.
  const skipRecenterRef = useRef(false);

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
    const tileLayer = L.tileLayer(tileUrlForScheme(computedRef.current), {
      attribution: CARTO_ATTRIBUTION,
      subdomains: 'abcd',
      maxZoom: 19,
    }).addTo(map);
    tileLayerRef.current = tileLayer;
    appliedSchemeRef.current = computedRef.current;
    // Убираем дефолтный префикс Leaflet (в 1.9 он содержит флаг Украины) —
    // оставляем только обязательную атрибуцию «© OpenStreetMap © CARTO».
    map.attributionControl.setPrefix(false);
    map.on('click', (e: L.LeafletMouseEvent) => {
      // Клик — пользователь сам выбрал и точку, и вид карты: маркер двигаем,
      // но карту НЕ рецентрим (иначе клик у края «прыгал» бы и зумил).
      skipRecenterRef.current = true;
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
      tileLayerRef.current = null;
      appliedSchemeRef.current = null;
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
    if (skipRecenterRef.current) {
      // Точку выставил клик по карте — вид не трогаем.
      skipRecenterRef.current = false;
    } else {
      // Внешнее изменение («Получить координаты» / ручной ввод) — центрируем
      // карту на точке, чтобы маркер был виден.
      map.setView(pos, PICK_ZOOM, { animate: true });
    }
  }, [latitude, longitude]);

  // Смена темы сайта на лету → переключаем тайлы (светлые/тёмные) без
  // пересоздания карты: setUrl меняет только источник тайлов «на месте»,
  // маркер/вид/клики не трогаются. Пропускаем, если схема не изменилась
  // (в т.ч. первый прогон — init-эффект уже поставил нужные тайлы).
  useEffect(() => {
    if (!tileLayerRef.current) return;
    if (appliedSchemeRef.current === computed) return;
    tileLayerRef.current.setUrl(tileUrlForScheme(computed));
    appliedSchemeRef.current = computed;
  }, [computed]);

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
