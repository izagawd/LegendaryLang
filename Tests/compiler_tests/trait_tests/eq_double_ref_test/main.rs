fn main() -> i32 {
    let a = 42;
    let ra = &a;
    let rra = &ra;
    let b = 42;
    let rb = &b;
    let rrb = &rb;
    if rra == rrb { 1 } else { 0 }
}
