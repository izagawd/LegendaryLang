fn main() -> i32 {
    let a = 10;
    let b = 10;
    let ra = &mut a;
    let rb = &mut b;
    let rra = &ra;
    let rrb = &rb;
    if rra == rrb { 1 } else { 0 }
}
