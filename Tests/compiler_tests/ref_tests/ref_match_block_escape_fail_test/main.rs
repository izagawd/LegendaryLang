enum Choice {
    A,
    B
}
fn main() -> i32 {
    let c = Choice.A;
    let r = match c {
        Choice.A => {
            let x = 5;
            &x
        },
        Choice.B => {
            let y = 10;
            &y
        }
    };
    *r
}
