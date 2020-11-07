import { render, screen } from "@testing-library/react";
import App from "./App";

test("renders splash screen on startup", () => {
  // Given
  const ui = <App />;

  // When
  render(ui);

  // Then
  const appTitle = screen.getByText(/frost alert/i);
  expect(appTitle).toBeInTheDocument();
});
