fn main() -> i32 {
    let x = 5;
    let r = &uniq x;
    let p = r;
    *r
}
