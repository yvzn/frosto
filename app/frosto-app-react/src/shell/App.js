import React from "react";

import SplashScreen from "./SplashScreen";

class App extends React.Component {
  constructor(props) {
    super(props);

    this.state = { status: "INIT" };
  }

  render() {
    return <>{this.state.status === "INIT" && <SplashScreen />}</>;
  }
}

export default App;
