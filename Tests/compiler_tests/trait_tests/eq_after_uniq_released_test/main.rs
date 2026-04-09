fn set(r: &uniq i32, val: i32) {
    *r = val;
}

fn main() -> i32 {
    let a = 5;
    set(&uniq a, 10);
    if a == 10 { 1 } else { 0 }
}
