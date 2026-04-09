fn main() -> i32 {
    let x = 10;
    let r = &uniq x;
    let val = *r;
    x + val
}
