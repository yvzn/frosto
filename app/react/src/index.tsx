import * as React from 'react';
import * as ReactDOM from "react-dom";

import App from './App';
import "./styles.scss";

var mountNode = document.querySelector("main");
ReactDOM.render(<App name="World" />, mountNode);
