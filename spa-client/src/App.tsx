import { Routes, Route } from "react-router-dom";
import Loader from "./Loader";
import { StructureView } from "./StructureView";
import { Defects } from "./Defects";

function App() {
  return (
    <Routes>
      <Route
        path="/"
        element={
          <Loader>
            <StructureView />
          </Loader>
        }
      />
      <Route
        path="/defects"
        element={
          <Loader>
            <Defects />
          </Loader>
        }
      />
    </Routes>
  );
}

export default App;
