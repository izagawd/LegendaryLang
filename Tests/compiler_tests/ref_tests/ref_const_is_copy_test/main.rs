fn takes_ref(r: &const i32) -> i32 {
    *r
}
fn main() -> i32 {
    let a = 5;
    let r = &const a;
    takes_ref(r) + takes_ref(r)
}
