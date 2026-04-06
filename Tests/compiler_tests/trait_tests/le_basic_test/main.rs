fn main() -> i32 {
    let r1 = if 3 <= 5 { 1 } else { 0 };
    let r2 = if 5 <= 5 { 10 } else { 0 };
    let r3 = if 7 <= 5 { 100 } else { 0 };
    r1 + r2 + r3
}
