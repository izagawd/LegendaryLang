struct Holder['a] {
    r: &'a i32
}
fn main() -> i32 {
    let a = 99;
    let h = make Holder { r : &a };
    *h.r
}
