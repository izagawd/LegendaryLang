fn input<T>(dd: &uniq T) -> &uniq T {
    dd
}
fn main() -> i32 {
    let num = 5;
    let derived = input(&uniq num);
    *derived
}
