fn main() -> i32 {
    let a = 5;
    let r1 = &uniq a;
    let r2 = &mut a;
    *r1 + *r2
}
