fn main() -> i32 {
    let r1 = if true != false { 1 } else { 0 };
    let r2 = if true != true { 10 } else { 0 };
    r1 + r2
}
