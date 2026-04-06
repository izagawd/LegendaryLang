fn takes_ref(r: &uniq i32) -> i32 {
    *r
}
fn main() -> i32 {
    let a = 5;
    let r = &uniq a;
    takes_ref(r);
    takes_ref(r)
}
