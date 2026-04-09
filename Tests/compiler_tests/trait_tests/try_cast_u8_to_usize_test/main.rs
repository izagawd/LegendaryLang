use Std.Primitive.TryCastPrimitive;

fn main() -> i32 {
    let x: u8 = 255;
    let result = TryCastPrimitive(usize, x);
    match result {
        Option.Some(val) => 1,
        Option.None => 0
    }
}
