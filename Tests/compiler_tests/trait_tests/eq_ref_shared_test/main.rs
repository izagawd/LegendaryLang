fn main() -> i32 {
    let a = 42;
    let b = 42;
    let c = 99;
    let ra = &a;
    let rb = &b;
    let rc = &c;
    let r1 = if ra == rb { 1 } else { 0 };
    let r2 = if ra == rc { 10 } else { 0 };
    r1 + r2
}
