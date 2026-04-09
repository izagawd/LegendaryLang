// &*Box.New(45) stored in a variable, then that variable is returned out of the
// block where the temporary lives. The temporary Box is dropped at the block's
// scope exit, making the reference dangle — this must be a compile error.
fn main() -> i32 {
    let r: &i32 = {
        &*Box.New(45)
    };
    *r
}
