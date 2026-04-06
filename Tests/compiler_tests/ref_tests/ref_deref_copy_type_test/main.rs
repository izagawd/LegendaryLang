fn main() -> i32 {
    let a = 5;
    let r = &a;
    let b = *r;
    let c = *r;
    b + c
}
