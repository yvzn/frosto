import { render, screen } from "@testing-library/react";
import Shell from "./Shell";

test("renders splash screen on startup", () => {
  // Given
  const props = {
    status: "INIT",
    location: undefined,
  };
  const ui = <Shell {...props} />;

  // When
  render(ui);

  // Then
  const appTitle = screen.getByText(/frost alert/i);
  expect(appTitle).toBeInTheDocument();
});
