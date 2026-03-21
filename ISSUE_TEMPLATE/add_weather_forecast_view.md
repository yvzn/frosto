## Task Overview

Add a new **Weather Forecast view** to the `app`, accessible at `/weather-forecast/:partitionKey/:rowKey`.

This view loads and displays the weather forecast for a given location, identified by its `partitionKey` and `rowKey` (matching the Azure Table Storage keys used by the API).

---

## Route

Register the following new route in `app/src/router/index.ts`:

```ts
{
  path: '/weather-forecast/:partitionKey/:rowKey',
  name: 'weather-forecast',
  component: () => import('../views/WeatherForecastView.vue'),
}
```

---

## New View: `WeatherForecastView.vue`

Create `app/src/views/WeatherForecastView.vue` with the following behaviour:

### Data Loading

- On mount, call the backend API endpoint `GET /api/weather-forecast?p={partitionKey}&r={rowKey}` using the base URL from a new `VITE_WEATHERFORECAST_URL` environment variable (to be added to `app/.env`).
- The API returns a JSON payload of the shape:
  ```json
  {
    "location": {
      "city": "...",
      "country": "...",
      "temperatureThreshold": 3.0
    },
    "forecasts": [ ... ]
  }
  ```

### Display

- Display the **location name** (`city`) and **country**.
- Display the **weather table** for the upcoming days (from `forecasts`).
- For each forecast row, display a frost/cold **icon** (❄️ or equivalent) when the temperature is **below the threshold** returned by the API (`location.temperatureThreshold`).

### Threshold Slider

- Provide a **slider input** allowing the user to manually adjust the temperature threshold.
- The slider should be initialised with the value returned by the API.
- Adjusting the slider updates which rows display the frost icon in real time, without reloading data.

### Error Handling

- If the API call **fails** (network error or non-2xx response), display an error message and a **retry button** that re-triggers the data fetch.
- If the API returns **401 / 403** or the keys are invalid / not found (400), **redirect** the user to the `/check-subscription` route.

---

## Environment Variable

Add `VITE_WEATHERFORECAST_URL=` to `app/.env` (following the existing pattern of `VITE_SIGNUP_URL`, `VITE_CHECKSUBSCRIPTION_URL`, etc.).

---

## i18n

Add translation keys for the new view in both `fr` and `en` locales, covering at minimum:
- Page/section title
- Loading state
- Error message
- Retry button label
- Threshold slider label (e.g. "Frost threshold")
- Table column headers (date, min temp, max temp, etc.)

---

## Acceptance Criteria

- [ ] Route `/weather-forecast/:partitionKey/:rowKey` is registered and navigates to the new view.
- [ ] The view fetches data from the API using the partition key and row key from the URL params.
- [ ] Location name and country are displayed.
- [ ] Weather table is displayed with forecast data for the next days.
- [ ] Frost icon is shown on rows where temperature is below the threshold.
- [ ] A slider lets the user manually adjust the threshold; the icon display updates in real time.
- [ ] On API error, an error message and a retry button are shown.
- [ ] On 400/401/403 or invalid keys, the user is redirected to `/check-subscription`.
- [ ] `VITE_WEATHERFORECAST_URL` is added to `app/.env`.
- [ ] i18n keys added for both `fr` and `en` locales.