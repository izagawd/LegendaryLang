struct Holder {
    r: &i32
}
fn main() -> i32 {
    let a = 99;
    let h = Holder { r = &a };
    *h.r
}
