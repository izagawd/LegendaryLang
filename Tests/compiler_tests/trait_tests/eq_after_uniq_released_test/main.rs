fn set(r: &mut i32, val: i32) {
    *r = val;
}

fn main() -> i32 {
    let a = 5;
    set(&mut a, 10);
    if a == 10 { 1 } else { 0 }
}
