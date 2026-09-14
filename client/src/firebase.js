
import { initializeApp } from "firebase/app";
import { getFirestore } from "firebase/firestore";


const firebaseConfig = {
  apiKey: "AIzaSyCHAmjsRWZfHbm1KTycOHHyAuTqKxEFUdk",
  authDomain: "taxi-system-9e0f5.firebaseapp.com",
  projectId: "taxi-system-9e0f5",
  storageBucket: "taxi-system-9e0f5.firebasestorage.app",
  messagingSenderId: "867465227354",
  appId: "1:867465227354:web:15aa9b410adbd1547efc63",
  measurementId: "G-KF45LDBX81"
};


const app = initializeApp(firebaseConfig);

export const db = getFirestore(app);