fn main() -> i32 {
    let a = true;
    let b = false;
    let c = true;
    let r1 = a && c;
    let r2 = a || b;
    let r3 = b && c;
    let r4 = b || b;
    let result = 0;
    if r1 { result = result + 1; };
    if r2 { result = result + 10; };
    if r3 { result = result + 100; };
    if r4 { result = result + 1000; };
    result
}
